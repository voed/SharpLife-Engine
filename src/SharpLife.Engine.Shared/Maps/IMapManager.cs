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

using SharpLife.FileFormats.BSP;
using System;

namespace SharpLife.Engine.Shared.Maps
{
    /// <summary>
    /// Manages the current map and all of its shared data
    /// Provides functions to load map data
    /// </summary>
    public interface IMapManager
    {
        /// <summary>
        /// The name of the currently loaded map, or null if no map is loaded
        /// </summary>
        string MapName { get; }

        /// <summary>
        /// Name of the previous map, or null if there was no previous map
        /// </summary>
        string PreviousMapName { get; }

        /// <summary>
        /// The currently loaded BSP file, if any
        /// </summary>
        BSPFile BSPFile { get; }

        /// <summary>
        /// Invoked when map data is being cleared
        /// </summary>
        event Action OnClear;

        /// <summary>
        /// Formats a map name as a file name
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        string FormatMapFileName(string mapName);

        /// <summary>
        /// Returns whether a map name is valid
        /// </summary>
        /// <param name="mapName">The name of the map, without directory or extension</param>
        /// <returns></returns>
        bool IsMapValid(string mapName);

        /// <summary>
        /// Attempts to load a map
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns>Whether the map was loaded</returns>
        bool LoadMap(string mapName);

        /// <summary>
        /// Clears all map data
        /// </summary>
        void Clear();

        /// <summary>
        /// Computes the CRC32 value for a given map
        /// </summary>
        /// <param name="mapName"></param>
        /// <param name="crc"></param>
        /// <returns></returns>
        bool ComputeCRC(string mapName, out uint crc);
    }
}
