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

using SharpLife.Engine.API.Game.Server;
using SharpLife.Game.Server.Entities;
using SharpLife.Game.Shared.Networking;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission;
using System;

namespace SharpLife.Game.Server.Networking
{
    internal sealed class ServerNetworking : IServerNetworking
    {
        private readonly ServerEntities _entities;

        public ServerNetworking(ServerEntities entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public void RegisterObjectListTypes(TypeRegistryBuilder typeRegistryBuilder)
        {
            SharedObjectListTypes.RegisterSharedTypes(typeRegistryBuilder);

            _entities.RegisterNetworkableEntities(typeRegistryBuilder);
        }

        public void CreateNetworkObjectLists(INetworkObjectListTransmitterBuilder networkObjectListTransmitterBuilder)
        {
            _entities.CreateNetworkObjectLists(networkObjectListTransmitterBuilder);
        }
    }
}
