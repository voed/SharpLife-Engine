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

using SharpLife.FileFormats.BSP;
using SharpLife.Models;
using SharpLife.Models.BSP;
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
using Veldrid.ImageSharp;
using Veldrid.Utilities;

namespace SharpLife.Renderer.BSP
{
    /// <summary>
    /// The BSP World model renderable
    /// </summary>
    public class BSPModelResourceContainer : ModelResourceContainer
    {
        private struct SingleFaceData : IDisposable
        {
            public Face Face;

            public uint FirstIndex;

            public uint IndicesCount;

            /// <summary>
            /// Up to <see cref="BSPConstants.MaxLightmaps"/> lightmap textures
            /// </summary>
            public ResourceSet Lightmaps;

            public void Dispose()
            {
                Lightmaps.Dispose();
            }
        }

        private class FaceBufferData : IDisposable
        {
            public ResourceSet Texture;

            public DeviceBuffer VertexBuffer;

            public DeviceBuffer IndexBuffer;

            public SingleFaceData[] Faces;

            public void Dispose()
            {
                Texture.Dispose();

                VertexBuffer.Dispose();
                IndexBuffer.Dispose();

                for (var i = 0; i < Faces.Length; ++i)
                {
                    Faces[i].Dispose();
                }
            }
        }

        private struct BSPSurfaceData
        {
            public WorldTextureCoordinate WorldTexture;
            public Vector2 Lightmap;

            public int Style0;
            public int Style1;
            public int Style2;
            public int Style3;
        }

        private readonly BSPModelResourceFactory _factory;
        private readonly BSPModel _bspModel;

        private DeviceBuffer _worldAndInverseBuffer;

        private List<FaceBufferData> _faces;
        private ResourceSet _sharedResourceSet;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public override IModel Model => _bspModel;

        public BSPModelResourceContainer(BSPModelResourceFactory factory, BSPModel bspModel)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _bspModel = bspModel ?? throw new ArgumentNullException(nameof(bspModel));
        }

        public override unsafe void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
            var wai = new WorldAndInverse(renderData.Origin, renderData.Angles, renderData.Scale);

            cl.UpdateBuffer(_worldAndInverseBuffer, 0, ref wai);

            cl.SetPipeline(_factory.Pipeline);
            cl.SetGraphicsResourceSet(0, _sharedResourceSet);

            var styles = stackalloc int[BSPConstants.MaxLightmaps];

            foreach (var faces in _faces)
            {
                cl.SetGraphicsResourceSet(1, faces.Texture);

                cl.SetVertexBuffer(0, faces.VertexBuffer);
                cl.SetIndexBuffer(faces.IndexBuffer, IndexFormat.UInt32);

                for (var i = 0; i < faces.Faces.Length; ++i)
                {
                    ref var face = ref faces.Faces[i];

                    cl.SetGraphicsResourceSet(2, face.Lightmaps);
                    cl.DrawIndexed(face.IndicesCount, 1, face.FirstIndex, 0, 0);
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

            _faces = new List<FaceBufferData>();

            foreach (var faces in sortedFaces)
            {
                if (faces.Key != "sky")
                {
                    //Don't use the dispose factory because some of the created resources are already managed elsewhere
                    var facesData = BuildFacesBuffer(faces.ToList(), sc.MapResourceCache, gd, cl, sc);
                    _disposeCollector.Add(facesData);
                    _faces.Add(facesData);
                }
            }

            _sharedResourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _factory.SharedLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                _worldAndInverseBuffer,
                _factory.LightStylesBuffer));
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();

            foreach (var faces in _faces)
            {
                faces.Dispose();
            }

            _faces.Clear();
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
        /// <param name="gd"></param>
        /// <param name="cl"></param>
        /// <param name="sc"></param>
        /// <param name="numLightmaps"></param>
        /// <param name="smax"></param>
        /// <param name="tmax"></param>
        /// <returns></returns>
        private Texture CreateLightmapTexture(Face face, ResourceCache resourceCache, GraphicsDevice gd, SceneContext sc, int numLightmaps, int smax, int tmax)
        {
            if (numLightmaps == 0)
            {
                //A white texture is used so when rendering surfaces with no lightmaps, the surface is fullbright
                //Since surfaces like these are typically triggers it makes things a lot easier
                return resourceCache.GetWhiteTexture(gd, gd.ResourceFactory);
            }

            using (var lightmapData = new Image<Rgba32>(numLightmaps * smax, tmax))
            {
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

                return resourceCache.AddTexture2D(
                    gd,
                    gd.ResourceFactory,
                    new ImageSharpTexture(lightmapData, false), $"lightmap{sc.MapResourceCache.GenerateUniqueId()}");
            }
        }

        private FaceBufferData BuildFacesBuffer(IReadOnlyList<Face> faces, ResourceCache resourceCache, GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (faces.Count == 0)
            {
                throw new ArgumentException("Cannot create a face buffer when no faces are provided");
            }

            var factory = gd.ResourceFactory;

            var mipTexture = faces[0].TextureInfo.MipTexture;

            var facesData = new List<SingleFaceData>();

            var vertices = new List<BSPSurfaceData>();
            var indices = new List<uint>();

            foreach (var face in faces)
            {
                var smax = (face.Extents[0] / 16) + 1;
                var tmax = (face.Extents[1] / 16) + 1;

                var firstVertex = vertices.Count;
                var firstIndex = indices.Count;

                //Create triangles out of the face
                foreach (var i in Enumerable.Range(firstVertex + 1, face.Points.Count - 2))
                {
                    indices.Add((uint)firstVertex);
                    indices.Add((uint)i);
                    indices.Add((uint)i + 1);
                }

                var textureInfo = face.TextureInfo;

                var numLightmaps = face.Styles.Count(style => style != BSPConstants.NoLightStyle);

                foreach (var point in face.Points)
                {
                    var s = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                    s /= mipTexture.Width;

                    var t = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                    t /= mipTexture.Height;

                    var lightmapS = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                    lightmapS -= face.TextureMins[0];
                    lightmapS += 8;
                    lightmapS /= smax * BSPConstants.LightmapScale;
                    lightmapS /= numLightmaps != 0 ? numLightmaps : 1; //Rescale X so it covers one lightmap in the texture

                    var lightmapT = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                    lightmapT -= face.TextureMins[1];
                    lightmapT += 8;
                    lightmapT /= tmax * BSPConstants.LightmapScale;

                    vertices.Add(new BSPSurfaceData
                    {
                        WorldTexture = new WorldTextureCoordinate
                        {
                            Vertex = point,
                            Texture = new Vector2(s, t)
                        },
                        Lightmap = new Vector2(lightmapS, lightmapT),
                        Style0 = face.Styles[0],
                        Style1 = face.Styles[1],
                        Style2 = face.Styles[2],
                        Style3 = face.Styles[3]
                    });
                }

                var lightmapTexture = CreateLightmapTexture(face, resourceCache, gd, sc, numLightmaps, smax, tmax);

                var lightmapView = resourceCache.GetTextureView(factory, lightmapTexture);

                facesData.Add(new SingleFaceData
                {
                    Face = face,
                    FirstIndex = (uint)firstIndex,
                    IndicesCount = (uint)(indices.Count - firstIndex),
                    Lightmaps = factory.CreateResourceSet(new ResourceSetDescription(
                        _factory.LightmapsLayout,
                        lightmapView
                    ))
                });
            }

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            var vb = factory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            cl.UpdateBuffer(vb, 0, verticesArray);

            var ib = factory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            cl.UpdateBuffer(ib, 0, indicesArray);

            var texture = resourceCache.GetTexture2D(mipTexture.Name);

            //If not found, fallback to have a valid texture
            texture = texture ?? resourceCache.GetPinkTexture(gd, factory);

            var view = resourceCache.GetTextureView(factory, texture);

            return new FaceBufferData
            {
                Texture = factory.CreateResourceSet(new ResourceSetDescription(
                    _factory.TextureLayout,
                    view,
                    sc.MainSampler)),
                VertexBuffer = vb,
                IndexBuffer = ib,
                Faces = facesData.ToArray()
            };
        }
    }
}
