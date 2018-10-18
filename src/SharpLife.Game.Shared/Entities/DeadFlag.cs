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
    public enum DeadFlag
    {
        /// <summary>
        /// Alive
        /// </summary>
        No = 0,

        /// <summary>
        /// Playing death animation or still falling off of a ledge waiting to hit ground
        /// </summary>
        Dying = 1,

        /// <summary>
        /// Dead. lying still
        /// </summary>
        Dead = 2,

        /// <summary>
        /// Dead, and can be respawned
        /// </summary>
        Respawnable = 3,

        /// <summary>
        /// Not used in the SDK, used by TFC for spies feigning death
        /// </summary>
        DiscardBody = 4,
    };
}
