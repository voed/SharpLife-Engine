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

namespace SharpLife.FileFormats.MDL
{
    [Flags]
    public enum TextureFlags
    {
        None = 0,
        FlatShade = 1 << 0,
        Chrome = 1 << 1,
        Fullbright = 1 << 2,
        NoMips = 1 << 3,
        Alpha = 1 << 4,
        Additive = 1 << 5,
        Masked = 1 << 6
    }
}
