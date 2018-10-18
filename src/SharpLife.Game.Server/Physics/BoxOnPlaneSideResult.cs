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

namespace SharpLife.Game.Server.Physics
{
    [Flags]
    public enum BoxOnPlaneSideResult
    {
        /// <summary>
        /// Should never be returned, box is neither in front nor behind the plane
        /// </summary>
        None = 0,

        /// <summary>
        /// The box is (partially) in front of the plane
        /// </summary>
        InFront = 1 << 0,

        /// <summary>
        /// The box is (partially) behind the plane
        /// </summary>
        Behind = 1 << 1,

        CrossesPlane = InFront | Behind
    };
}
