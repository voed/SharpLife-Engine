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
    public enum EffectsFlags
    {
        None = 0,

        /// <summary>
        /// swirling cloud of particles
        /// </summary>
        BrightField = 1 << 0,

        /// <summary>
        /// single frame ELIGHT on entity attachment 0
        /// </summary>
        MuzzleFlash = 1 << 1,

        /// <summary>
        /// DLIGHT centered at entity origin
        /// </summary>
        BrightLight = 1 << 2,

        /// <summary>
        /// player flashlight
        /// </summary>
        DimLight = 1 << 3,

        /// <summary>
        /// get lighting from ceiling
        /// </summary>
        InvLight = 1 << 4,

        /// <summary>
        /// don't interpolate the next frame
        /// </summary>
        NoInterpolation = 1 << 5,

        /// <summary>
        /// rocket flare glow sprite
        /// </summary>
        Light = 1 << 6,

        /// <summary>
        /// don't draw entity
        /// </summary>
        Nodraw = 1 << 7,

        /// <summary>
        /// player nightvision
        /// </summary>
        NightVision = 1 << 8,

        /// <summary>
        /// sniper laser effect
        /// </summary>
        SniperLaser = 1 << 9,

        /// <summary>
        /// fiber camera
        /// </summary>
        FiberCamera = 1 << 10
    }
}
