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

namespace SharpLife.FileFormats.BSP
{
    public enum PlaneType
    {
        // 0-2 are axial planes
        X = 0,
        Y = 1,
        Z = 2,

        // 3-5 are non-axial planes snapped to the nearest
        AnyX = 3,
        AnyY = 4,
        AnyZ = 5,
    }
}
