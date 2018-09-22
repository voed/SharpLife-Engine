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

namespace SharpLife.Game.Client.Renderer.Shared.Models
{
    /// <summary>
    /// Manages the model graphics resources
    /// </summary>
    public interface IModelResourcesManager
    {
        /// <summary>
        /// Returns whether the resources for a given model are loaded
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool IsLoaded(IModel model);

        /// <summary>
        /// Creates the resources for a given model
        /// </summary>
        /// <param name="model"></param>
        ModelResourceContainer CreateResources(IModel model);

        /// <summary>
        /// Gets the resource container for a given model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        ModelResourceContainer GetResources(IModel model);

        /// <summary>
        /// Frees all model resources
        /// </summary>
        void FreeAllResources();
    }
}
