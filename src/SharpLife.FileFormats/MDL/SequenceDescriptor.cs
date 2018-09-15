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

using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.FileFormats.MDL
{
    public class SequenceDescriptor
    {
        public struct Blend
        {
            public MotionTypes Type;
            public float Start;
            public float End;
        }

        public string Name { get; set; }

        public float FPS { get; set; }

        public SequenceFlags Flags { get; set; }

        public int Activity { get; set; }

        public int ActivityWeight { get; set; }

        public List<Event> Events { get; set; }

        public int FrameCount { get; set; }

        public MotionTypes MotionType { get; set; }

        public int MotionBone { get; set; }

        public Vector3 LinearMovement { get; set; }

        public Vector3 BBMin { get; set; }

        public Vector3 BBMax { get; set; }

        public List<AnimationBlend> AnimationBlends { get; set; }

        public Blend[] Blends { get; } = new Blend[MDLConstants.NumBlendTypes];

        public int EntryNode { get; set; }

        public int ExitNode { get; set; }

        public TransitionNodeFlags NodeFlags { get; set; }
        //TODO
    }
}
