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
        private const int NoLightStyle = -1;

        private struct SingleFaceData : IDisposable
        {
            public Face Face;

            public int[] CachedLightStyles;

            public uint FirstIndex;

            public uint IndicesCount;

            /// <summary>
            /// Up to <see cref="BSPConstants.MaxLightmaps"/> lightmap textures
            /// </summary>
            public ResourceSet Lightmaps;

            public DeviceBuffer Styles;

            public void Dispose()
            {
                Lightmaps.Dispose();
                Styles.Dispose();
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

                    for (var style = 0; style < BSPConstants.MaxLightmaps; ++style)
                    {
                        var styleIndex = face.Face.Styles[style];

                        var styleValue = styleIndex != BSPConstants.NoLightStyle ? _factory.LightStyles.GetStyleValue(styleIndex) : NoLightStyle;

                        //Cache style values to reduce the number of updates
                        if (face.CachedLightStyles[style] != styleValue)
                        {
                            face.CachedLightStyles[style] = styleValue;

                            //Convert to normalized [0, 1] range
                            var inputValue = styleValue != NoLightStyle ? styleValue / 255.0f : NoLightStyle;

                            cl.UpdateBuffer(face.Styles, (uint)(Marshal.SizeOf<float>() * style), ref inputValue);
                        }
                    }

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
                _worldAndInverseBuffer));
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

        private Texture GenerateLightmap(GraphicsDevice gd, SceneContext sc, Face face, int lightmapIndex)
        {
            var smax = (face.Extents[0] / BSPConstants.LightmapScale) + 1;
            var tmax = (face.Extents[1] / BSPConstants.LightmapScale) + 1;

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

            using (var image = Image.LoadPixelData(colorData, smax, tmax))
            {
                return sc.MapResourceCache.AddTexture2D(gd, gd.ResourceFactory, new ImageSharpTexture(image, false), $"lightmap{sc.MapResourceCache.GenerateUniqueId()}");
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
                        Lightmap = new Vector2(lightmapS, lightmapT)
                    });
                }

                var resources = new List<BindableResource>(BSPConstants.MaxLightmaps);

                var styles = face.Styles.Select(value => (int)value).ToArray();

                for (var i = 0; i < BSPConstants.MaxLightmaps; ++i)
                {
                    //Use pink texture if no style is provided
                    var lightmap = face.Styles[i] != 255 ? GenerateLightmap(gd, sc, face, i) : sc.MapResourceCache.GetPinkTexture(gd, factory);

                    resources.Add(sc.MapResourceCache.GetTextureView(factory, lightmap));
                }

                var stylesBuffer = factory.CreateBuffer(new BufferDescription((uint)(Marshal.SizeOf<float>() * BSPConstants.MaxLightmaps), BufferUsage.UniformBuffer));

                resources.Add(stylesBuffer);

                facesData.Add(new SingleFaceData
                {
                    Face = face,
                    CachedLightStyles = new int[BSPConstants.MaxLightmaps] { NoLightStyle, NoLightStyle, NoLightStyle, NoLightStyle },
                    FirstIndex = (uint)firstIndex,
                    IndicesCount = (uint)(indices.Count - firstIndex),
                    Lightmaps = factory.CreateResourceSet(new ResourceSetDescription(
                        _factory.LightmapsLayout,
                        resources.ToArray()
                    )),
                    Styles = stylesBuffer
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
