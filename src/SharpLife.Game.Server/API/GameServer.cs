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
using SharpLife.Game.Server.Networking;
using System;

namespace SharpLife.Game.Server.API
{
    public class GameServer : IGameServer
    {
        private ServerNetworking _networking;

        private IServiceProvider _serviceProvider;

        public void Initialize(IServiceCollection serviceCollection)
        {
            //Expose as both to get the implementation
            serviceCollection.AddSingleton<ServerNetworking>();
            serviceCollection.AddSingleton<IServerNetworking>(provider => provider.GetRequiredService<ServerNetworking>());
        }

        public void Startup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _networking = serviceProvider.GetRequiredService<ServerNetworking>();
        }

        public void Shutdown()
        {
        }
    }
}
