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

using Microsoft.Extensions.DependencyInjection;
using SharpLife.Models;
using System;
using System.Collections.Generic;

namespace SharpLife.Engine.Shared.API.Game.Server
{
    public interface IGameServer
    {
        void Initialize(IServiceCollection serviceCollection);

        void Startup(IServiceProvider serviceProvider);

        void Shutdown();

        IReadOnlyList<IModelLoader> GetModelLoaders();

        /// <summary>
        /// Returns whether the given map name is valid
        /// </summary>
        /// <param name="mapName">The map name without directory or extension</param>
        /// <returns></returns>
        bool IsMapValid(string mapName);

        /// <summary>
        /// Begin loading the map
        /// Check for existence of the file, load it and verify integrity
        /// Load embedded models
        /// </summary>
        /// <param name="mapName"></param>
        /// <param name="flags"></param>
        bool TryMapLoadBegin(string mapName, ServerStartFlags flags);

        /// <summary>
        /// Continue loading the map
        /// </summary>
        /// <param name="loadGame"></param>
        void MapLoadContinue(bool loadGame);

        /// <summary>
        /// Called when map loading has finished
        /// </summary>
        void MapLoadFinished();

        void Activate();

        void Deactivate();

        /// <summary>
        /// Called at the start of a game frame
        /// </summary>
        void StartFrame();

        /// <summary>
        /// Called at the end of a game frame
        /// </summary>
        void EndFrame();
    }
}
