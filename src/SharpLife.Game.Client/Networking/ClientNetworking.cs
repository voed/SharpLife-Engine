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

using Google.Protobuf;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Engine.Shared.API.Game.Client;
using SharpLife.Game.Client.API;
using SharpLife.Game.Client.Entities;
using SharpLife.Game.Shared.Networking;
using SharpLife.Game.Shared.Networking.Messages.Server;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using System;

namespace SharpLife.Game.Client.Networking
{
    internal sealed class ClientNetworking : IClientNetworking
    {
        private readonly IEngineModels _engineModels;
        private readonly GameClient _gameClient;
        private readonly ClientEntities _entities;

        public ClientNetworking(IEngineModels engineModels, GameClient gameClient, ClientEntities entities)
        {
            _engineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
            _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public void ProcessGameInfoMessage(ByteString encodedMessage)
        {
            var message = GameServerInfo.Parser.ParseFrom(encodedMessage);

            //Cache off the name so we can look it up later
            _gameClient.CachedMapName = message.MapFileName;
        }

        public void RegisterObjectListTypes(TypeRegistryBuilder typeRegistryBuilder)
        {
            SharedObjectListTypes.RegisterSharedTypes(typeRegistryBuilder, _engineModels);

            _entities.RegisterNetworkableEntities(typeRegistryBuilder);
        }

        public void CreateNetworkObjectLists(INetworkObjectListReceiverBuilder networkObjectListBuilder)
        {
            _entities.CreateNetworkObjectLists(networkObjectListBuilder);
        }
    }
}
