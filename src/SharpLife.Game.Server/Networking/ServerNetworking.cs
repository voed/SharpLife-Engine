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

using SharpLife.Engine.API.Engine.Shared;
using SharpLife.Engine.API.Game.Server;
using SharpLife.Game.Shared;
using SharpLife.Game.Shared.Networking;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;

namespace SharpLife.Game.Server.Networking
{
    internal sealed class ServerNetworking : IServerNetworking
    {
        private INetworkObjectList _entitiesList;

        public void RegisterObjectListTypes(TypeRegistry typeRegistry)
        {
            SharedObjectListTypes.RegisterSharedTypes(typeRegistry);
        }

        public void CreateNetworkObjectLists(IEngineNetworkObjectLists engineObjectLists)
        {
            _entitiesList = engineObjectLists.CreateList(GameConstants.NetworkObjectLists.EntitiesListName);
        }
    }
}
