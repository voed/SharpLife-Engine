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

namespace SharpLife.Game.Shared.Entities
{
    public enum WaterLevel
    {
        /// <summary>
        /// Not in water at all
        /// </summary>
        Dry = 0,

        /// <summary>
        /// Standing in water, feet only
        /// </summary>
        Feet = 1,

        /// <summary>
        /// Halfway submerged
        /// </summary>
        Waist = 2,

        /// <summary>
        /// Submerged up to eyes or more
        /// </summary>
        Head = 3
    }
}
