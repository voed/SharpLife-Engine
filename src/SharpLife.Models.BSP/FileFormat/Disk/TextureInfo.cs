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


namespace SharpLife.Models.BSP.FileFormat.Disk
{
    internal unsafe struct TextureInfo
    {
#pragma warning disable CS0649
        public fixed float vecs[8];		// [s/t][xyz offset]
        public int miptex;
        public TextureFlags flags;
#pragma warning restore CS0649
    }
}
