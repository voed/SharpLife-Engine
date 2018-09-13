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

using SharpLife.Models;
using SharpLife.Renderer.Models;
using System;
using Veldrid;

namespace SharpLife.Renderer.StudioModel
{
    public sealed class StudioModelResourceContainer : ModelResourceContainer
    {
        private readonly SharpLife.Models.Studio.StudioModel _studioModel;

        public override IModel Model => _studioModel;

        public StudioModelResourceContainer(SharpLife.Models.Studio.StudioModel studioModel)
        {
            _studioModel = studioModel ?? throw new ArgumentNullException(nameof(studioModel));
        }

        //TODO: implement

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
        }
    }
}
