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

namespace SharpLife.FileFormats.WAD
{
    public enum WADVersion : uint
    {
        WAD2 = (byte)'W' | (byte)'A' << 8 | (byte)'D' << 16 | (byte)'2' << 24,
        WAD3 = (byte)'W' | (byte)'A' << 8 | (byte)'D' << 16 | (byte)'3' << 24
    }
}
