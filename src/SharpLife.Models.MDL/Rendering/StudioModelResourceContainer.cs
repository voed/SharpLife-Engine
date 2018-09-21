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

using SharpLife.Game.Client.Renderer.Shared;
using SharpLife.Game.Client.Renderer.Shared.Models;
using SharpLife.Models.MDL.Loading;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using System;
using System.Collections.Generic;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Models.MDL.Rendering
{
    public sealed class StudioModelResourceContainer : ModelResourceContainer
    {
        private readonly StudioModelResourceFactory _factory;

        private readonly StudioModel _studioModel;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private ResourceSet _sharedResourceSet;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        private ResourceSet[] _textures;

        private BodyPartData[] _bodyParts;

        public override IModel Model => _studioModel;

        public StudioModelResourceContainer(StudioModelResourceFactory factory, StudioModel studioModel)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _studioModel = studioModel ?? throw new ArgumentNullException(nameof(studioModel));
        }

        //TODO: implement

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
            var wai = new WorldAndInverse(renderData.Origin, renderData.Angles, renderData.Scale);

            sc.UpdateWorldAndInverseBuffer(cl, ref wai);

            var studioRenderData = new StudioModelRenderData
            {
                Frame = renderData.Frame
            };

            var bones = _factory.BoneCalculator.SetUpBones(_studioModel.StudioFile, studioRenderData);

            cl.UpdateBuffer(_factory.BonesBuffer, 0, bones);

            cl.SetPipeline(_factory.Pipeline);

            cl.SetGraphicsResourceSet(0, _sharedResourceSet);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

            foreach (var bodyPart in _bodyParts)
            {
                var subModel = bodyPart.SubModels[0];

                foreach (var mesh in subModel.Meshes)
                {
                    cl.SetGraphicsResourceSet(1, _textures[_studioModel.StudioFile.Skins[0][mesh.Mesh.Skin]]);

                    cl.DrawIndexed(mesh.IndicesCount, 1, mesh.StartIndex, 0, 0);
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

            var vertices = new List<StudioVertex>();
            var indices = new List<uint>();

            //Construct the meshes for each body part
            var bodyParts = new List<BodyPartData>(_studioModel.StudioFile.BodyParts.Count);

            foreach (var bodyPart in _studioModel.StudioFile.BodyParts)
            {
                bodyParts.Add(StudioResourceUtils.CreateBodyPart(_studioModel.StudioFile, bodyPart, vertices, indices));
            }

            _bodyParts = bodyParts.ToArray();

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            _vertexBuffer = disposeFactory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            gd.UpdateBuffer(_vertexBuffer, 0, verticesArray);

            _indexBuffer = disposeFactory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            gd.UpdateBuffer(_indexBuffer, 0, indicesArray);

            _sharedResourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _factory.SharedLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                sc.WorldAndInverseBuffer,
                _factory.BonesBuffer,
                sc.MainSampler
                ));

            var uploadedTextures = StudioResourceUtils.CreateTextures(_studioModel.Name, _studioModel.StudioFile, gd, sc.TextureLoader, sc.MapResourceCache);

            _textures = new ResourceSet[uploadedTextures.Count];

            for (var i = 0; i < uploadedTextures.Count; ++i)
            {
                var view = sc.MapResourceCache.GetTextureView(gd.ResourceFactory, uploadedTextures[i]);

                _textures[i] = disposeFactory.CreateResourceSet(new ResourceSetDescription(_factory.TextureLayout, view));
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
