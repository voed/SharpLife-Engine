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

using SharpLife.Engine.API.Game.Client;
using SharpLife.Game.Client.Entities;
using SharpLife.Game.Shared.Networking;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using System;

namespace SharpLife.Game.Client.Networking
{
    internal sealed class ClientNetworking : IClientNetworking
    {
        private readonly ClientEntities _entities;

        public ClientNetworking(ClientEntities entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public void RegisterObjectListTypes(TypeRegistryBuilder typeRegistryBuilder)
        {
            SharedObjectListTypes.RegisterSharedTypes(typeRegistryBuilder);

            _entities.RegisterNetworkableEntities(typeRegistryBuilder);
        }

        public void CreateNetworkObjectLists(INetworkObjectListReceiverBuilder networkObjectListBuilder)
        {
            _entities.CreateNetworkObjectLists(networkObjectListBuilder);
        }
    }
}
