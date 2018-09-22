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

using SharpLife.Game.Shared.Models.SPR;
using SharpLife.Models;
using SharpLife.Models.SPR.Rendering;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Models.SPR
{
    public class SpriteModelResourceContainer : ModelResourceContainer
    {
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public override IModel Model => SpriteModel;

        public SpriteModel SpriteModel { get; }

        public DeviceBuffer[] VertexBuffers { get; set; }
        public DeviceBuffer IndexBuffer { get; set; }
        public DeviceBuffer RenderColorBuffer { get; set; }
        public ResourceSet ResourceSet { get; set; }

        public SpriteModelResourceContainer(SpriteModel spriteModel)
        {
            SpriteModel = spriteModel ?? throw new ArgumentNullException(nameof(spriteModel));
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            (var atlasImage, var vertexBuffers) = SPRResourceUtils.CreateSpriteAtlas(SpriteModel.SpriteFile, gd, disposeFactory, sc.TextureLoader);

            //TODO: disable mipmapping for certain sprites?
            var texture = sc.MapResourceCache.AddTexture2D(gd, disposeFactory, new ImageSharpTexture(atlasImage, true), $"{SpriteModel.Name}_atlas");

            VertexBuffers = vertexBuffers;

            IndexBuffer = SPRResourceUtils.CreateIndexBuffer(gd, disposeFactory);

            RenderColorBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<Vector4>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var view = sc.MapResourceCache.GetTextureView(gd.ResourceFactory, texture);

            ResourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                sc.ModelRenderer.SpriteRenderer.Layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                sc.WorldAndInverseBuffer,
                view,
                sc.MainSampler,
                sc.LightingInfoBuffer,
                RenderColorBuffer));
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
            VertexBuffers = null;
        }
    }
}
