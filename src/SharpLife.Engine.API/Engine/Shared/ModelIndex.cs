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

namespace SharpLife.Engine.API.Engine.Shared
{
    /// <summary>
    /// Represents a strongly typed model index
    /// Do not compare instances of these to determine if they refer to the same model, use the appropriate API method
    /// </summary>
    public struct ModelIndex : IEquatable<ModelIndex>
    {
        /// <summary>
        /// The invalid index
        /// Entities with this index have no model
        /// </summary>
        public const int InvalidIndex = 0;

        public int Index { get; }

        public bool Valid => Index != InvalidIndex;

        public ModelIndex(int index)
        {
            Index = index;
        }

        public override bool Equals(object obj)
        {
            return obj is ModelIndex modelIndex && Equals(modelIndex);
        }

        public bool Equals(ModelIndex other)
        {
            return Index == other.Index
                   && Valid == other.Valid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Valid);
        }

        public static bool operator ==(ModelIndex index1, ModelIndex index2)
        {
            return index1.Equals(index2);
        }

        public static bool operator !=(ModelIndex index1, ModelIndex index2)
        {
            return !(index1 == index2);
        }
    }
}
