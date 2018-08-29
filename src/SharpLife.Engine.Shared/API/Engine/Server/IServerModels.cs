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

namespace SharpLife.Engine.Shared.API.Engine.Server
{
    /// <summary>
    /// Provides access to the server's model data
    /// </summary>
    public interface IServerModels
    {
        /// <summary>
        /// Loads a model
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        IModel LoadModel(string modelName);

        /// <summary>
        /// Gets a model by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IModel GetModel(in ModelIndex index);

        /// <summary>
        /// Gets the index of a model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        ModelIndex IndexOf(IModel model);

        /// <summary>
        /// Compares two model indices and returns whether they refer to the same model, or if they are both the invalid index
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IndicesEqual(in ModelIndex lhs, in ModelIndex rhs);
    }
}
