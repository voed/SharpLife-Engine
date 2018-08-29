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
using Veldrid;

namespace SharpLife.Renderer.Models
{
    /// <summary>
    /// Resource container for a model
    /// </summary>
    public abstract class ModelResourceContainer : ResourceContainer
    {
        /// <summary>
        /// The model whose resources this container manages
        /// </summary>
        public abstract IModel Model { get; }

        /// <summary>
        /// Render this model with the given data
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="cl"></param>
        /// <param name="sc"></param>
        /// <param name="renderPass"></param>
        /// <param name="renderData"></param>
        public abstract void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData);
    }
}
