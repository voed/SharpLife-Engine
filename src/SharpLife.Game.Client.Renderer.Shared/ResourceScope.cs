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

using System;

namespace SharpLife.Game.Client.Renderer.Shared
{
    [Flags]
    public enum ResourceScope
    {
        /// <summary>
        /// Resources allocated for use over the duration of the program's lifetime
        /// </summary>
        Global = 1 << 0,

        /// <summary>
        /// Resources allocated for one map. Freed when the map ends
        /// </summary>
        Map = 1 << 1,

        All = Global | Map
    }
}
