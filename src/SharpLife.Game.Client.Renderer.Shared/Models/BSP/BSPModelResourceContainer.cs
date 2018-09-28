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

using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Models;
using SharpLife.Models.BSP.Rendering;
using SharpLife.Renderer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public override IModel Model => BSPModel;

        public BSPModel BSPModel { get; }

        public DeviceBuffer VertexBuffer { get; set; }
        public DeviceBuffer IndexBuffer { get; set; }
        public SingleLightmapData[] Lightmaps { get; set; }

        public BSPModelResourceContainer(BSPModel bspModel)
        {
            BSPModel = bspModel ?? throw new ArgumentNullException(nameof(bspModel));
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
            var sortedFaces = BSPModel.SubModel.Faces.GroupBy(face => face.TextureInfo.MipTexture.Name);

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

                        var textureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(sc.ModelRenderer.BrushRenderer.TextureLayout, view, sc.MainSampler));

                        BSPResourceUtils.BuildFacesBuffer(
                            BSPModel.BSPFile,
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

            Lightmaps = lightmapBuilders
                    .Select(builder => builder.Build(sc.ModelRenderer.BrushRenderer.LightmapLayout, sc.MapResourceCache, gd, gd.ResourceFactory))
                    .ToArray();

            Array.ForEach(Lightmaps, lightmap => _disposeCollector.Add(lightmap));

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            VertexBuffer = disposeFactory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            cl.UpdateBuffer(VertexBuffer, 0, verticesArray);

            IndexBuffer = disposeFactory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            cl.UpdateBuffer(IndexBuffer, 0, indicesArray);
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
