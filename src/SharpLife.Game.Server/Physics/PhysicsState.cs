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

using SharpLife.Game.Shared.Physics;
using System;

namespace SharpLife.Game.Server.Physics
{
    public sealed class PhysicsState
    {
        private readonly short[] _leafNums = new short[PhysicsConstants.MaxLeafs];

        public int LeafCount { get; private set; }

        public int HeadNode { get; set; }

        public uint GroupInfo { get; set; }

        public AreaNode Area { get; set; }

        public short GetLeafNumber(int index) => _leafNums[index];

        public void AddLeafNumber(short number)
        {
            if (LeafCount < PhysicsConstants.MaxLeafs)
            {
                _leafNums[LeafCount++] = number;
            }
            else
            {
                LeafCount = PhysicsConstants.MaxLeafs + 1;
            }
        }

        public void ClearNodeState()
        {
            LeafCount = 0;
            HeadNode = -1;
        }

        public void MarkLeafCountOverflowed(int topNode)
        {
            LeafCount = 0;
            HeadNode = topNode;
            Array.Fill(_leafNums, (short)255);
        }

        public void CopyNodeStateFrom(PhysicsState other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            HeadNode = other.HeadNode;
            LeafCount = other.LeafCount;

            Array.Copy(other._leafNums, _leafNums, _leafNums.Length);
        }
    }
}
