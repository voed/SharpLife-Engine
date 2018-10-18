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

namespace SharpLife.Game.Shared.Entities
{
    [Flags]
    public enum EntityFlags
    {
        None = 0,
        PendingDestruction = 1 << 0,

        /// <summary>
        /// This entity is a client
        /// </summary>
        Client = 1 << 1,

        Monster = 1 << 2,

        MonsterClip = 1 << 3,

        WorldBrush = 1 << 4,

        OnGround = 1 << 5,

        AlwaysThink = 1 << 6,

        Float = 1 << 7,

        Fly = 1 << 8,

        Swim = 1 << 9,

        Conveyor = 1 << 10,

        BaseVelocity = 1 << 11,

        ImmuneWater = 1 << 12,

        GodMode = 1 << 13,

        InWater = 1 << 14,

        ImmuneLava = 1 << 15,

        ImmuneSlime = 1 << 16,

        WaterJump = 1 << 17,
    }
}
