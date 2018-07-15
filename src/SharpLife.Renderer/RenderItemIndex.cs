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

namespace SharpLife.Renderer
{
    public struct RenderItemIndex : IComparable<RenderItemIndex>, IComparable
    {
        public RenderOrderKey Key { get; }
        public int ItemIndex { get; }

        public RenderItemIndex(RenderOrderKey key, int itemIndex)
        {
            Key = key;
            ItemIndex = itemIndex;
        }

        public int CompareTo(object obj)
        {
            return ((IComparable)Key).CompareTo(obj);
        }

        public int CompareTo(RenderItemIndex other)
        {
            return Key.CompareTo(other.Key);
        }

        public override string ToString()
        {
            return string.Format("Index:{0}, Key:{1}", ItemIndex, Key);
        }
    }
}
