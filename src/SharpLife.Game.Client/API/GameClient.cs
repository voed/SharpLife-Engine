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
using SharpLife.Engine.Shared.API.Game.Client;
using SharpLife.Game.Client.Entities;
using SharpLife.Game.Client.Networking;
using SharpLife.Game.Client.UI;
using SharpLife.Game.Shared.Models;
using SharpLife.Models;
using System;
using System.Collections.Generic;

namespace SharpLife.Game.Client.API
{
    public sealed class GameClient : IGameClient
    {
        private ClientNetworking _networking;

        private ClientEntities _entities;

        public void Initialize(IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddSingleton<IClientUI, ImGuiInterface>();

            //Expose as both to get the implementation
            serviceCollection.AddSingleton<ClientNetworking>();
            serviceCollection.AddSingleton<IClientNetworking>(provider => provider.GetRequiredService<ClientNetworking>());
            serviceCollection.AddSingleton<ClientEntities>();
            serviceCollection.AddSingleton<IClientEntities>(provider => provider.GetRequiredService<ClientEntities>());
        }

        public void Startup(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _networking = serviceProvider.GetRequiredService<ClientNetworking>();

            _entities = serviceProvider.GetRequiredService<ClientEntities>();

            _entities.Startup();
        }

        public void Shutdown()
        {
        }

        public IReadOnlyList<IModelLoader> GetModelLoaders() => GameModelUtils.GetModelLoaders();

        public void MapLoadBegin(string entityData)
        {
            _entities.MapLoadBegin();
        }

        public void MapLoadFinished()
        {

        }

        public void MapShutdown()
        {
            _entities.MapShutdown();
        }
    }
}
