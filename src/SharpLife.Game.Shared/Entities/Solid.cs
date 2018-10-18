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
    /// <summary>
    /// <see cref="SharedBaseEntity.Solid"/> values
    /// NOTE: Some movetypes will cause collisions independent of SOLID_NOT/SOLID_TRIGGER when the entity moves
    /// SOLID only effects OTHER entities colliding with this one when they move - UGH!
    /// </summary>
    public enum Solid
    {
        /// <summary>
        /// no interaction with other objects
        /// </summary>
        Not = 0,

        /// <summary>
        /// touch on edge, but not blocking
        /// </summary>
        Trigger = 1,

        /// <summary>
        /// touch on edge, block
        /// </summary>
        BBox = 2,

        /// <summary>
        /// touch on edge, but not an onground
        /// </summary>
        SlideBox = 3,

        /// <summary>
        /// bsp clip, touch on edge, block
        /// </summary>
        BSP = 4,
    }
}
