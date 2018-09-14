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

namespace SharpLife.FileFormats.MDL.Disk
{
    public unsafe struct SequenceDescriptor
    {
        internal const int LabelSize = 32;

        internal fixed byte Label[LabelSize];

#pragma warning disable CS0649
        internal float FPS;
        internal int Flags;

        internal int Activity;
        internal int ActivityWeight;

        internal int NumEvents;
        internal int EventIndex;

        internal int FrameCount;

        //Written to file, but never used
        internal int NumPivots;
        internal int PivotIndex;

        internal int MotionType;
        internal int MotionBone;
        internal Vector3 LinearMovement;
        internal int AutoMovePosIndex;
        internal int AutoMoveAngleIndex;

        internal Vector3 BBMin;
        internal Vector3 BBMax;

        internal int NumBlends;
        internal int AnimIndex;

        internal fixed int BlendType[2];
        internal fixed float BlendStart[2];
        internal fixed float BlendEnd[2];
        internal int BlendParent;

        internal int SeqGroup;

        internal int EntryNode;
        internal int ExitNode;
        internal int NodeFlags;

        internal int NextSeq;
#pragma warning restore CS0649
    }
}
