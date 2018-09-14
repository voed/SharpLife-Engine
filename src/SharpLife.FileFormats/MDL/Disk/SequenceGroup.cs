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

namespace SharpLife.FileFormats.MDL.Disk
{
    internal unsafe struct SequenceGroup
    {
        internal const int LabelSize = 32;
        internal const int NameSize = 64;

#pragma warning disable CS0649
        internal fixed byte Label[LabelSize];

        internal fixed byte Name[NameSize];

        internal int Cache;

        internal int Data;
#pragma warning restore CS0649
    }
}
