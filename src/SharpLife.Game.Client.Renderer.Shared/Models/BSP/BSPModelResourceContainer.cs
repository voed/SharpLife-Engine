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

using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Models;
using SharpLife.Models.BSP.Rendering;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Models.BSP
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

        private readonly BSPModelResourceFactory _factory;
        private readonly BSPModel _bspModel;

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

            sc.UpdateWorldAndInverseBuffer(cl, ref wai);

            var renderArguments = new BSPRenderArguments
            {
                RenderColor = GetBrushColor(ref renderData) / 255.0f,
                RenderMode = renderData.RenderMode
            };

            cl.UpdateBuffer(_factory.RenderArgumentsBuffer, 0, ref renderArguments);

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

            //A white texture is used so when rendering surfaces with no lightmaps, the surface is fullbright
            //Since surfaces like these are typically triggers it makes things a lot easier
            using (var defaultLightmapTexture = Image.LoadPixelData(new[] { Rgba32.White }, 1, 1))
            {
                foreach (var faces in sortedFaces)
                {
                    if (faces.Key != "sky")
                    {
                        var facesList = faces.ToList();

                        var mipTexture = facesList[0].TextureInfo.MipTexture;

                        var texture = sc.MapResourceCache.GetTexture2D(mipTexture.Name);

                        //If not found, fallback to have a valid texture
                        texture = texture ?? sc.MapResourceCache.GetPinkTexture(gd, gd.ResourceFactory);

                        var view = sc.MapResourceCache.GetTextureView(gd.ResourceFactory, texture);

                        var textureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_factory.TextureLayout, view, sc.MainSampler));

                        BSPResourceUtils.BuildFacesBuffer(
                            _bspModel.BSPFile,
                            facesList,
                            mipTexture,
                            textureResourceSet,
                            defaultLightmapTexture,
                            lightmapBuilders,
                            vertices,
                            indices);
                    }
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
                sc.WorldAndInverseBuffer,
                sc.LightingInfoBuffer,
                sc.LightStylesBuffer,
                _factory.RenderArgumentsBuffer));
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
        }
    }
}
