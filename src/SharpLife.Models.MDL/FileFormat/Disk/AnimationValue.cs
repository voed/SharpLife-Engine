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

using System.Runtime.InteropServices;

namespace SharpLife.Models.MDL.FileFormat.Disk
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct AnimationValue
    {
        internal struct Number
        {
#pragma warning disable CS0649
            internal byte Valid;
            internal byte Total;
#pragma warning restore CS0649
        }

        [FieldOffset(0)]
        internal Number Num;

        [FieldOffset(0)]
        internal short Value;
    }
}
