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
using System.Collections.Generic;

namespace SharpLife.Models
{
    /// <summary>
    /// Manages the models that have been loaded for a map
    /// </summary>
    public interface IModelManager : IEnumerable<IModel>
    {
        /// <summary>
        /// Gets a model by name
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        IModel this[string modelName] { get; }

        /// <summary>
        /// Gets the number of models that have been loaded
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the fallback model
        /// </summary>
        IModel FallbackModel { get; }

        /// <summary>
        /// Invoked when a new model is loaded
        /// </summary>
        event Action<IModel> OnModelLoaded;

        /// <summary>
        /// Returns whether a model with the given name has been loaded
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        bool Contains(string modelName);

        /// <summary>
        /// Loads a model
        /// If the model was already loaded, it is returned without any additional action
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        IModel Load(string modelName);

        /// <summary>
        /// Loads the fallback model, if it isn't already loaded
        /// </summary>
        /// <param name="fallbackModelName"></param>
        /// <returns></returns>
        IModel LoadFallbackModel(string fallbackModelName);

        /// <summary>
        /// Clears all model data
        /// </summary>
        void Clear();
    }
}
