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

namespace SharpLife.Engine.Shared.API.Game.Client
{
    public interface IGameClient
    {
        /// <summary>
        /// Allows the client to add services to the shared engine/client service collection
        /// </summary>
        /// <param name="serviceCollection"></param>
        void Initialize(IServiceCollection serviceCollection);

        /// <summary>
        /// Allows the client to query for services exposed by the engine and itself
        /// </summary>
        /// <param name="serviceProvider"></param>
        void Startup(IServiceProvider serviceProvider);

        void Shutdown();

        IReadOnlyList<IModelLoader> GetModelLoaders();

        /// <summary>
        /// Begin loading the map
        /// </summary>
        void MapLoadBegin();

        void MapLoadFinished();

        void MapShutdown();

        void Update(float deltaSeconds);

        void Draw();
    }
}
