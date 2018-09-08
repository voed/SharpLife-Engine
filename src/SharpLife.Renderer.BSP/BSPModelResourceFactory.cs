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
using SharpLife.Models.BSP;
using SharpLife.Renderer.Models;
using System;
using Veldrid;

namespace SharpLife.Renderer.BSP
{
    public sealed class BSPModelResourceFactory : IModelResourceFactory
    {
        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
        }

        public void DestroyDeviceObjects(ResourceScope scope)
        {
        }

        public void Dispose()
        {
            DestroyDeviceObjects(ResourceScope.All);
        }

        public ModelResourceContainer CreateContainer(IModel model)
        {
            if (!(model is BSPModel bspModel))
            {
                throw new ArgumentException("Model must be a BSP model", nameof(model));
            }

            return new BSPModelRenderable(bspModel);
        }
    }
}
