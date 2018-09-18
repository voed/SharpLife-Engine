/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.BSP.Loading;
using SharpLife.Renderer;
using SharpLife.Renderer.Models;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;
using static SharpLife.Models.BSP.Rendering.BSPModelResourceFactory;

namespace SharpLife.Models.BSP.Rendering
{
    /// <summary>
    /// The BSP World model renderable
    /// </summary>
    public class BSPModelResourceContainer : ModelResourceContainer
    {
        /// <summary>
        /// A good tradeoff between getting as few lightmaps as possible, without creating overly large textures
        /// </summary>
        private const int LightmapPoolSize = 512;

        private struct SingleTextureData : IDisposable
        {
            public ResourceSet Texture;

            public uint FirstIndex;

            public uint IndicesCount;

            public void Dispose()
            {
                Texture.Dispose();
            }
        }

        private struct SingleLightmapData : IDisposable
        {
            public ResourceSet Lightmap;

            public SingleTextureData[] Textures;

            public void Dispose()
            {
                Lightmap.Dispose();

                for (var i = 0; i < Textures.Length; ++i)
                {
                    Textures[i].Dispose();
                }
            }
        }

        private sealed class LightmapBuilder
        {
            private readonly LightmapPool _pool;

            private readonly List<SingleTextureData> _textures = new List<SingleTextureData>();

            public int Width => _pool.Width;

            public int Height => _pool.Height;

            public LightmapBuilder(int width, int height)
            {
                _pool = new LightmapPool(width, height);
            }

            public Vector2? TryAllocate(Image<Rgba32> newImageData)
            {
                return _pool.TryAllocate(newImageData);
            }

            public void AddTextureData(SingleTextureData textureData)
            {
                _textures.Add(textureData);
            }

            public SingleLightmapData Build(ResourceLayout layout, ResourceCache resourceCache, GraphicsDevice gd, ResourceFactory factory)
            {
                var texture = _pool.Upload(resourceCache, gd, factory);

                _pool.Dispose();

                return new SingleLightmapData
                {
                    Lightmap = factory.CreateResourceSet(new ResourceSetDescription(
                        layout,
                        resourceCache.GetTextureView(factory, texture)
                    )),
                    Textures = _textures.ToArray()
                };
            }
        }

        private struct BSPSurfaceData
        {
            public WorldTextureCoordinate WorldTexture;
            public Vector2 Lightmap;

            public float LightmapXOffset;

            public int Style0;
            public int Style1;
            public int Style2;
            public int Style3;
        }

        private readonly BSPModelResourceFactory _factory;
        private readonly BSPModel _bspModel;

        private DeviceBuffer _worldAndInverseBuffer;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        private SingleLightmapData[] _lightmaps;
        private ResourceSet _sharedResourceSet;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public override IModel Model => _bspModel;

        public BSPModelResourceContainer(BSPModelResourceFactory factory, BSPModel bspModel)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _bspModel = bspModel ?? throw new ArgumentNullException(nameof(bspModel));
        }

        private static Vector4 GetBrushColor(ref ModelRenderData renderData)
        {
            switch (renderData.RenderMode)
            {
                case RenderMode.Normal:
                case RenderMode.TransAlpha:
                    return new Vector4(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

                case RenderMode.TransColor:
                    return new Vector4(renderData.RenderColor, renderData.RenderAmount);

                case RenderMode.TransTexture:
                case RenderMode.Glow:
                    return new Vector4(byte.MaxValue, byte.MaxValue, byte.MaxValue, renderData.RenderAmount);

                case RenderMode.TransAdd:
                    return new Vector4(renderData.RenderAmount, renderData.RenderAmount, renderData.RenderAmount, byte.MaxValue);

                default: throw new InvalidOperationException($"Render mode {renderData.RenderMode} not supported");
            }
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
            var wai = new WorldAndInverse(renderData.Origin, renderData.Angles, renderData.Scale);

            cl.UpdateBuffer(_worldAndInverseBuffer, 0, ref wai);

            var renderArguments = new RenderArguments
            {
                RenderColor = GetBrushColor(ref renderData) / 255.0f,
                RenderMode = renderData.RenderMode
            };

            cl.UpdateBuffer(_factory.RenderColorBuffer, 0, ref renderArguments);

            var pipeline = _factory.Pipelines[renderData.RenderMode];

            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, _sharedResourceSet);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

            for (var lightmapIndex = 0; lightmapIndex < _lightmaps.Length; ++lightmapIndex)
            {
                ref var lightmap = ref _lightmaps[lightmapIndex];

                cl.SetGraphicsResourceSet(2, lightmap.Lightmap);

                for (var textureIndex = 0; textureIndex < lightmap.Textures.Length; ++textureIndex)
                {
                    ref var texture = ref lightmap.Textures[textureIndex];

                    cl.SetGraphicsResourceSet(1, texture.Texture);

                    cl.DrawIndexed(texture.IndicesCount, 1, texture.FirstIndex, 0, 0);
                }
            }
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            _worldAndInverseBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<WorldAndInverse>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            //Build a list of buffers, each buffer containing all of the faces that have the same texture
            //This reduces the number of buffers we have to create from a set for each face to a set for each texture and all of the faces referencing it
            //TODO: further split by visleaf when vis data is available
            var sortedFaces = _bspModel.SubModel.Faces.GroupBy(face => face.TextureInfo.MipTexture.Name);

            sortedFaces = sortedFaces.OrderByDescending(grouping => grouping.Count());

            var lightmapBuilders = new List<LightmapBuilder>
            {
                new LightmapBuilder(LightmapPoolSize, LightmapPoolSize)
            };

            var vertices = new List<BSPSurfaceData>();
            var indices = new List<uint>();

            foreach (var faces in sortedFaces)
            {
                if (faces.Key != "sky")
                {
                    //Don't use the dispose factory because some of the created resources are already managed elsewhere
                    BuildFacesBuffer(
                        faces.ToList(),
                        sc.MapResourceCache,
                        gd,
                        sc,
                        lightmapBuilders,
                        vertices,
                        indices);
                }
            }

            _lightmaps = lightmapBuilders
                    .Select(builder => builder.Build(_factory.LightmapLayout, sc.MapResourceCache, gd, gd.ResourceFactory))
                    .ToArray();

            Array.ForEach(_lightmaps, lightmap => _disposeCollector.Add(lightmap));

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            _vertexBuffer = disposeFactory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            cl.UpdateBuffer(_vertexBuffer, 0, verticesArray);

            _indexBuffer = disposeFactory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            cl.UpdateBuffer(_indexBuffer, 0, indicesArray);

            _sharedResourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _factory.SharedLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                _worldAndInverseBuffer,
                sc.LightingInfoBuffer,
                _factory.LightStylesBuffer,
                _factory.RenderColorBuffer));
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
        }

        private Image<Rgba32> GenerateLightmap(Face face, int smax, int tmax, int lightmapIndex)
        {
            var size = smax * tmax;

            var colorData = new Rgba32[size];

            if (_bspModel.BSPFile.Lighting.Length > 0 && face.LightOffset != -1)
            {
                //Initialize from light data
                var lightmapData = new Span<Rgb24>(_bspModel.BSPFile.Lighting, face.LightOffset + (size * lightmapIndex), size);

                for (var i = 0; i < size; ++i)
                {
                    //Lightmap data is passed directly to shaders; shaders will handle light styles and gamma correction
                    lightmapData[i].ToRgba32(ref colorData[i]);
                }
            }
            else
            {
                //Fill with fullbright
                for (var i = 0; i < size; ++i)
                {
                    colorData[i].R = byte.MaxValue;
                    colorData[i].G = byte.MaxValue;
                    colorData[i].B = byte.MaxValue;
                    colorData[i].A = byte.MaxValue;
                }
            }

            return Image.LoadPixelData(colorData, smax, tmax);
        }

        /// <summary>
        /// Create the lightmap texture for a surface
        /// If the surface has no lightmaps, returns white texture
        /// </summary>
        /// <param name="face"></param>
        /// <param name="resourceCache"></param>
        /// <param name="numLightmaps"></param>
        /// <param name="smax"></param>
        /// <param name="tmax"></param>
        /// <returns></returns>
        private Image<Rgba32> CreateLightmapTexture(Face face, int numLightmaps, int smax, int tmax)
        {
            if (numLightmaps == 0)
            {
                //A white texture is used so when rendering surfaces with no lightmaps, the surface is fullbright
                //Since surfaces like these are typically triggers it makes things a lot easier
                var pixels = new Rgba32[] { Rgba32.White };
                return Image.LoadPixelData(pixels, pixels.Length, pixels.Length);
            }

            var lightmapData = new Image<Rgba32>(numLightmaps * smax, tmax);

            var graphicsOptions = GraphicsOptions.Default;

            graphicsOptions.BlenderMode = PixelBlenderMode.Src;

            //Generate lightmap data for every style
            for (var i = 0; i < numLightmaps; ++i)
            {
                using (var styleData = GenerateLightmap(face, smax, tmax, i))
                {
                    lightmapData.Mutate(context => context.DrawImage(graphicsOptions, styleData, new Point(i * smax, 0)));
                }
            }

            return lightmapData;
        }

        private void BuildFacesBuffer(
            IReadOnlyList<Face> faces,
            ResourceCache resourceCache,
            GraphicsDevice gd,
            SceneContext sc,
            List<LightmapBuilder> lightmapBuilders,
            List<BSPSurfaceData> vertices,
            List<uint> indices)
        {
            if (faces.Count == 0)
            {
                throw new ArgumentException("Cannot create a face buffer when no faces are provided");
            }

            var factory = gd.ResourceFactory;

            var mipTexture = faces[0].TextureInfo.MipTexture;

            var texture = resourceCache.GetTexture2D(mipTexture.Name);

            //If not found, fallback to have a valid texture
            texture = texture ?? resourceCache.GetPinkTexture(gd, factory);

            var view = resourceCache.GetTextureView(factory, texture);

            var lightmapBuilder = lightmapBuilders[lightmapBuilders.Count - 1];

            var firstVertex = vertices.Count;
            var firstIndex = indices.Count;

            void AddTextureData()
            {
                lightmapBuilder.AddTextureData(new SingleTextureData
                {
                    Texture = factory.CreateResourceSet(new ResourceSetDescription(
                            _factory.TextureLayout,
                            view,
                            sc.MainSampler)),
                    FirstIndex = (uint)firstIndex,
                    IndicesCount = (uint)(indices.Count - firstIndex)
                });
            }

            foreach (var face in faces)
            {
                var smax = (face.Extents[0] / 16) + 1;
                var tmax = (face.Extents[1] / 16) + 1;

                var numLightmaps = face.Styles.Count(style => style != BSPConstants.NoLightStyle);

                using (var lightmapTexture = CreateLightmapTexture(face, numLightmaps, smax, tmax))
                {
                    var coordinates = lightmapBuilder.TryAllocate(lightmapTexture);

                    if (!coordinates.HasValue)
                    {
                        //Lightmap is full

                        //Add the current vertices to the full one
                        AddTextureData();

                        //New starting point
                        firstIndex = indices.Count;

                        //Create a new one
                        lightmapBuilder = new LightmapBuilder(lightmapBuilder.Width, lightmapBuilder.Height);
                        lightmapBuilders.Add(lightmapBuilder);

                        //This can't fail without throwing an exception
                        coordinates = lightmapBuilder.TryAllocate(lightmapTexture);
                    }

                    //Create triangles out of the face
                    foreach (var i in Enumerable.Range(vertices.Count + 1, face.Points.Count - 2))
                    {
                        indices.Add((uint)vertices.Count);
                        indices.Add((uint)i);
                        indices.Add((uint)i + 1);
                    }

                    var textureInfo = face.TextureInfo;

                    foreach (var point in face.Points)
                    {
                        var s = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                        s /= mipTexture.Width;

                        var t = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                        t /= mipTexture.Height;

                        var lightmapS = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                        lightmapS -= face.TextureMins[0];
                        lightmapS += coordinates.Value.X * BSPConstants.LightmapScale;
                        lightmapS += 8;
                        lightmapS /= lightmapBuilder.Width * BSPConstants.LightmapScale;
                        //lightmapS /= numLightmaps != 0 ? numLightmaps : 1; //Rescale X so it covers one lightmap in the texture

                        var lightmapT = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                        lightmapT -= face.TextureMins[1];
                        lightmapT += coordinates.Value.Y * BSPConstants.LightmapScale;
                        lightmapT += 8;
                        lightmapT /= lightmapBuilder.Height * BSPConstants.LightmapScale;

                        vertices.Add(new BSPSurfaceData
                        {
                            WorldTexture = new WorldTextureCoordinate
                            {
                                Vertex = point,
                                Texture = new Vector2(s, t)
                            },
                            Lightmap = new Vector2(lightmapS, lightmapT),
                            LightmapXOffset = smax / (float)lightmapBuilder.Width,
                            Style0 = face.Styles[0],
                            Style1 = face.Styles[1],
                            Style2 = face.Styles[2],
                            Style3 = face.Styles[3]
                        });
                    }
                }
            }

            AddTextureData();
        }
    }
}
