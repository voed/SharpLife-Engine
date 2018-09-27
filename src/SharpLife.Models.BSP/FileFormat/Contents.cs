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

namespace SharpLife.Models.BSP.FileFormat
{
    /// <summary>
    /// contents of a spot in the world
    /// </summary>
    public enum Contents
    {
        /// <summary>
        /// Indicates that this is a node
        /// </summary>
        Node = 0,

        Empty = -1,
        Solid = -2,
        Water = -3,
        Slime = -4,
        Lava = -5,
        Sky = -6,

        Origin = -7,        // removed at csg time
        Clip = -8,          // changed to contents_solid
        Current0 = -9,
        Current90 = -10,
        Current180 = -11,
        Current270 = -12,
        CurrentUp = -13,
        CurrentDown = -14,
        Translucent = -15,

        Ladder = -16,
    }
}
