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

using SharpLife.CommandSystem;
using SharpLife.Engine.Shared.API.Game.Server;
using SharpLife.Utility.Events;

namespace SharpLife.Engine.Server.Host
{
    /// <summary>
    /// The server host, responsible for all engine level server operations
    /// </summary>
    public interface IEngineServerHost
    {
        /// <summary>
        /// The command context used by the server
        /// </summary>
        ICommandContext CommandContext { get; }

        /// <summary>
        /// The event system used by the server
        /// </summary>
        IEventSystem EventSystem { get; }

        /// <summary>
        /// Whether the server is running
        /// </summary>
        bool Active { get; }

        void Shutdown();

        /// <summary>
        /// Starts a new server on the given map
        /// TODO: remove startspot and use generic data carryover to pass data between maps
        /// </summary>
        /// <param name="mapName"></param>
        /// <param name="startSpot">Name of the entity where players should respawn</param>
        /// <param name="flags"></param>
        bool Start(string mapName, string startSpot = null, ServerStartFlags flags = ServerStartFlags.None);

        void InitializeMap(ServerStartFlags flags);

        void Activate();

        void Deactivate();

        void Stop();

        void RunFrame(float deltaSeconds);

        /// <summary>
        /// Returns whether the given map name is valid
        /// </summary>
        /// <param name="mapName">The map name without directory or extension</param>
        /// <returns></returns>
        bool IsMapValid(string mapName);
    }
}
