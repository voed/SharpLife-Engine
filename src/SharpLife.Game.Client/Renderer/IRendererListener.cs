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

namespace SharpLife.Game.Client.Renderer
{
    public interface IRendererListener
    {
        /// <summary>
        /// Invoked when models should be rendered
        /// </summary>
        /// <param name="modelRenderer"></param>
        /// <param name="viewState"></param>
        void OnRenderModels(IModelRenderer modelRenderer, IViewState viewState);
    }
}
