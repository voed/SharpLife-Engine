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

using SharpLife.Game.Shared.Models.BSP;

namespace SharpLife.Game.Shared.Maps
{
    /// <summary>
    /// Provides access to map info
    /// </summary>
    public interface IMapInfo
    {
        /// <summary>
        /// The name of the map, excluding directory and extension
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Name of the previous map that was loaded, or null if no previous map was loaded
        /// </summary>
        string PreviousMapName { get; }

        /// <summary>
        /// The map model
        /// </summary>
        BSPModel Model { get; }
    }
}
