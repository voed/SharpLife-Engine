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

using SharpLife.Utility.Events;
using System;

namespace SharpLife.Engine.Shared.Events
{
    public class EngineEvents : IEventList
    {
        /// <summary>
        /// A new map request has been given
        /// </summary>
        public const string EngineNewMapRequest = nameof(EngineNewMapRequest);

        /// <summary>
        /// Valid new map, starting server
        /// </summary>
        public const string EngineStartingServer = nameof(EngineStartingServer);

        /// <summary>
        /// Dispatched right before map data starts loading
        /// </summary>
        public const string ServerMapDataStartLoad = nameof(ServerMapDataStartLoad);

        /// <summary>
        /// Dispatched right after map data finished loading
        /// </summary>
        public const string ServerMapDataFinishLoad = nameof(ServerMapDataFinishLoad);

        /// <summary>
        /// Dispatched right after the map's CRC has been computed
        /// </summary>
        public const string ServerMapCRCComputed = nameof(ServerMapCRCComputed);

        /// <summary>
        /// Dispatched right before entities start loading
        /// </summary>
        public const string ServerMapEntitiesStartLoad = nameof(ServerMapEntitiesStartLoad);

        /// <summary>
        /// Dispatched right after entities finished loading
        /// </summary>
        public const string ServerMapEntitiesFinishLoad = nameof(ServerMapEntitiesFinishLoad);

        /// <summary>
        /// Dispatched right before the server activates the game
        /// </summary>
        public const string ServerActivatePreGameActivate = nameof(ServerActivatePreGameActivate);

        /// <summary>
        /// Dispatched right after the server activates the game
        /// </summary>
        public const string ServerActivatePostGameActivate = nameof(ServerActivatePostGameActivate);

        /// <summary>
        /// Dispatched right after the server activates Steam
        /// </summary>
        public const string ServerActivatePostSteamActivate = nameof(ServerActivatePostSteamActivate);

        /// <summary>
        /// Dispatched right after the server creates generic resources
        /// </summary>
        public const string ServerActivatePostCreateGenericResources = nameof(ServerActivatePostCreateGenericResources);

        /// <summary>
        /// Dispatched right before the client starts connecting to a server
        /// </summary>
        public const string ClientStartConnect = nameof(ClientStartConnect);

        /// <summary>
        /// Dispatched right before the client starts the disconnect action
        /// </summary>
        public const string ClientStartDisconnect = nameof(ClientStartDisconnect);

        /// <summary>
        /// Dispatched right after the client has sent the disconnect command to the server
        /// </summary>
        public const string ClientDisconnectSent = nameof(ClientDisconnectSent);

        /// <summary>
        /// Dispatched right after the client has finished disconnecting
        /// </summary>
        public const string ClientEndDisconnect = nameof(ClientEndDisconnect);

        public string[] SimpleEvents => new[]
        {
            EngineNewMapRequest,
            EngineStartingServer,
            ServerMapDataStartLoad,
            ServerMapDataFinishLoad,
            ServerMapCRCComputed,
            ServerMapEntitiesStartLoad,
            ServerMapEntitiesFinishLoad,

            ServerActivatePreGameActivate,
            ServerActivatePostGameActivate,
            ServerActivatePostSteamActivate,
            ServerActivatePostCreateGenericResources,

            ClientStartConnect,
            ClientStartDisconnect,
            ClientDisconnectSent,
            ClientEndDisconnect,
        };

        public Type[] EventTypes => new Type[]
        {
            typeof(MapStartedLoading),
        };
    }
}
