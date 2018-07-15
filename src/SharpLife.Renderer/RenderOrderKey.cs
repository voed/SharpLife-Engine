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

using System;
using System.Runtime.CompilerServices;

namespace SharpLife.Renderer
{
    public struct RenderOrderKey : IComparable<RenderOrderKey>, IComparable
    {
        public readonly ulong Value;

        public RenderOrderKey(ulong value)
        {
            Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderOrderKey Create(int materialID, float cameraDistance)
            => Create((uint)materialID, cameraDistance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderOrderKey Create(uint materialID, float cameraDistance)
        {
            uint cameraDistanceInt = (uint)Math.Min(uint.MaxValue, cameraDistance * 1000f);

            return new RenderOrderKey(
                ((ulong)materialID << 32)
                + cameraDistanceInt);
        }

        public int CompareTo(RenderOrderKey other)
        {
            return Value.CompareTo(other.Value);
        }

        int IComparable.CompareTo(object obj)
        {
            return Value.CompareTo(obj);
        }
    }
}