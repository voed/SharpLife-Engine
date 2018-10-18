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

using SharpLife.Game.Server.Entities;
using SharpLife.Game.Shared.Physics;
using System.Numerics;

namespace SharpLife.Game.Server.Physics
{
    public struct MoveClip
    {
        public Vector3 BoxMins, BoxMaxs;// enclose the test object along entire move

        public Vector3 Mins, Maxs;  // size of the moving object
        public Vector3 Mins2, Maxs2;    // size when clipping against monsters

        public Vector3 Start, End;
        public Trace Trace;
        public TraceType Type;
        public bool IgnoreTransparent;
        public BaseEntity PassEntity;
        public bool MonsterClipBrush;
    }
}
