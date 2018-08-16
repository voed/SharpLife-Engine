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
using SharpLife.Engine.API.Game.Server;
using System;

namespace SharpLife.Game.Server.API
{
    public class GameServer : IGameServer
    {
        private IServiceProvider _serviceProvider;

        public void Initialize(IServiceCollection serviceCollection)
        {
        }

        public void Startup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Shutdown()
        {
        }
    }
}
