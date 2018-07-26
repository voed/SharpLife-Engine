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
    /// <summary>
    /// The server has started loading a new map
    /// </summary>
    public class MapStartedLoading : EventData
    {
        public string MapName { get; }

        /// <summary>
        /// Name of the previous map, or null if there was no previous map
        /// </summary>
        public string OldMapName { get; }

        /// <summary>
        /// Whether this is a new map or a changelevel
        /// </summary>
        public bool NewMap { get; }

        /// <summary>
        /// Whether this map is being loaded from a saved game
        /// </summary>
        public bool LoadingSavedGame { get; }

        public MapStartedLoading(string mapName, string oldMapName, bool newMap, bool loadingSavedGame)
        {
            MapName = mapName ?? throw new ArgumentNullException(nameof(mapName));
            OldMapName = oldMapName;
            NewMap = newMap;
            LoadingSavedGame = loadingSavedGame;
        }
    }
}
