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

using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Models;
using SharpLife.Models.MDL.Rendering;
using SharpLife.Renderer;
using System;
using System.Collections.Generic;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Models.MDL
{
    public sealed class StudioModelResourceContainer : ModelResourceContainer
    {
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public override IModel Model => StudioModel;

        public StudioModel StudioModel { get; }

        public DeviceBuffer VertexBuffer { get; set; }
        public DeviceBuffer IndexBuffer { get; set; }

        public ResourceSet[] Textures { get; set; }
        public BodyPartData[] BodyParts { get; set; }

        public StudioModelResourceContainer(StudioModel studioModel)
        {
            StudioModel = studioModel ?? throw new ArgumentNullException(nameof(studioModel));
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            var vertices = new List<StudioVertex>();
            var indices = new List<uint>();

            //Construct the meshes for each body part
            var bodyParts = new List<BodyPartData>(StudioModel.StudioFile.BodyParts.Count);

            foreach (var bodyPart in StudioModel.StudioFile.BodyParts)
            {
                bodyParts.Add(StudioResourceUtils.CreateBodyPart(StudioModel.StudioFile, bodyPart, vertices, indices));
            }

            BodyParts = bodyParts.ToArray();

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            VertexBuffer = disposeFactory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            gd.UpdateBuffer(VertexBuffer, 0, verticesArray);

            IndexBuffer = disposeFactory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            gd.UpdateBuffer(IndexBuffer, 0, indicesArray);

            var uploadedTextures = StudioResourceUtils.CreateTextures(StudioModel.Name, StudioModel.StudioFile, gd, sc.TextureLoader, sc.MapResourceCache);

            Textures = new ResourceSet[uploadedTextures.Count];

            for (var i = 0; i < uploadedTextures.Count; ++i)
            {
                var view = sc.MapResourceCache.GetTextureView(gd.ResourceFactory, uploadedTextures[i]);

                Textures[i] = disposeFactory.CreateResourceSet(new ResourceSetDescription(sc.ModelRenderer.StudioRenderer.TextureLayout, view));
            }
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
