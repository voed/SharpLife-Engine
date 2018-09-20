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
using SharpLife.Engine.Shared.API.Game.Server;
using SharpLife.Game.Server.API;
using SharpLife.Game.Server.Entities;
using SharpLife.Game.Shared.Networking;
using SharpLife.Game.Shared.Networking.Messages.Server;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission;
using System;

namespace SharpLife.Game.Server.Networking
{
    internal sealed class ServerNetworking : IServerNetworking
    {
        private readonly IEngineModels _engineModels;
        private readonly GameServer _gameServer;
        private readonly ServerEntities _entities;

        public ServerNetworking(IEngineModels engineModels, GameServer gameServer, ServerEntities entities)
        {
            _engineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
            _gameServer = gameServer ?? throw new ArgumentNullException(nameof(gameServer));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public IMessage CreateGameInfoMessage()
        {
            return new GameServerInfo
            {
                //In case the file format/directory ever changes, use the full file name
                MapFileName = NetUtilities.ConvertToNetworkPath(_gameServer.MapInfo.Model.Name),
                MapCrc = _gameServer.MapInfo.Model.CRC,
                AllowCheats = false, //TODO: define cvar
            };
        }

        public void RegisterObjectListTypes(TypeRegistryBuilder typeRegistryBuilder)
        {
            SharedObjectListTypes.RegisterSharedTypes(typeRegistryBuilder, _engineModels);

            _entities.RegisterNetworkableEntities(typeRegistryBuilder);
        }

        public void CreateNetworkObjectLists(INetworkObjectListTransmitterBuilder networkObjectListBuilder)
        {
            _entities.CreateNetworkObjectLists(networkObjectListBuilder);
        }
    }
}
