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
using SharpLife.Engine.Shared.Maps;

namespace SharpLife.Engine.Shared.API.Engine.Server
{
    public interface IServerEngine
    {
        ICommandContext CommandContext { get; }

        /// <summary>
        /// Gets the current map info instance
        /// Don't cache this, it gets recreated every map
        /// </summary>
        IMapInfo MapInfo { get; }

        /// <summary>
        /// Returns whether the given map name is valid
        /// </summary>
        /// <param name="mapName">The map name without directory or extension</param>
        /// <returns></returns>
        bool IsMapValid(string mapName);
    }
}
