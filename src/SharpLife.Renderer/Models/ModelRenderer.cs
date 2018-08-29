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

using System;
using System.Numerics;
using Veldrid;

namespace SharpLife.Renderer.Models
{
    /// <summary>
    /// Responsible for rendering models
    /// </summary>
    public sealed class ModelRenderer : IModelRenderer, IRenderable
    {
        public delegate void RenderModels(IModelRenderer modelRenderer);

        private readonly IModelResourcesManager _resourcesManager;

        private readonly RenderModels _renderModels;

        private bool _active;

        private RenderContext _renderContext;

        public ModelRenderer(IModelResourcesManager resourcesManager, RenderModels renderModelsCallback)
        {
            _resourcesManager = resourcesManager ?? throw new ArgumentNullException(nameof(resourcesManager));
            _renderModels = renderModelsCallback ?? throw new ArgumentNullException(nameof(renderModelsCallback));
        }

        public void Render(ref ModelRenderData renderData)
        {
            if (renderData.Model == null)
            {
                throw new ArgumentNullException(nameof(renderData), $"{nameof(renderData.Model)} cannot be null");
            }

            if (!_active)
            {
                throw new InvalidOperationException($"Cannot call {nameof(Render)} outside the render operation");
            }

            var resources = _resourcesManager.GetResources(renderData.Model);

            resources.Render(_renderContext.GraphicsDevice, _renderContext.CommandList, _renderContext.SceneContext, _renderContext.RenderPass, ref renderData);
        }

        public RenderPasses RenderPasses => RenderPasses.Standard;

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey();
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            _active = true;
            _renderContext = new RenderContext { GraphicsDevice = gd, CommandList = cl, SceneContext = sc, RenderPass = renderPass };

            _renderModels(this);

            _renderContext = new RenderContext();
            _active = false;
        }
    }
}
