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

using System.Numerics;

namespace SharpLife.Models.BSP.FileFormat.Disk
{
    internal unsafe struct Model
    {
#pragma warning disable CS0649
        public Vector3 mins, maxs;
        public Vector3 origin;
        public fixed int headnode[BSPConstants.MaxHulls];
        public int visleafs;       // not including the solid leaf 0
        public int firstface, numfaces;
#pragma warning restore CS0649
    }
}
