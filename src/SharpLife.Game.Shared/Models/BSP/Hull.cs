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

using SharpLife.Models.BSP.FileFormat;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.Game.Shared.Models.BSP
{
    public class Hull
    {
        public int FirstClipNode;

        public int LastClipNode;

        public Vector3 ClipMins;

        public Vector3 ClipMaxs;

        public IReadOnlyList<ClipNode> ClipNodes;

        public Memory<SharpLife.Models.BSP.FileFormat.Plane> Planes;

        public Hull(
            int firstClipNode, int lastClipNode,
            in Vector3 clipMins, in Vector3 clipMaxs,
            IReadOnlyList<ClipNode> clipNodes,
            Memory<SharpLife.Models.BSP.FileFormat.Plane> planes)
        {
            FirstClipNode = firstClipNode;
            LastClipNode = lastClipNode;
            ClipMins = clipMins;
            ClipMaxs = clipMaxs;
            ClipNodes = clipNodes ?? throw new ArgumentNullException(nameof(clipNodes));
            Planes = planes;
        }
    }
}
