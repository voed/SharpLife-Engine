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

namespace SharpLife.Models.MDL.FileFormat
{
    [Flags]
    public enum MotionTypes
    {
        None = 0,

        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,

        XR = 1 << 3,
        YR = 1 << 4,
        ZR = 1 << 5,

        LX = 1 << 6,
        LY = 1 << 7,
        LZ = 1 << 8,

        AX = 1 << 9,
        AY = 1 << 10,
        AZ = 1 << 11,

        AXR = 1 << 12,
        AYR = 1 << 13,
        AZR = 1 << 14,

        Types = X | Y | Z
            | XR | YR | ZR
            | LX | LY | LZ
            | AX | AY | AZ
            | AXR | AYR | AZR,

        RLoop = 1 << 15
    }
}
