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
using System.Collections.Generic;

namespace SharpLife.FileFormats.MDL
{
    public class Animation
    {
        public struct AnimationValue
        {
            public short Value;
            public int Count;
        }

        /// <summary>
        /// List of animation values for a specific frame for each axis
        /// Compressed by storing repeated values as one entry
        /// </summary>
        public List<AnimationValue>[] Values { get; } = new List<AnimationValue>[MDLConstants.NumAxes];

        public bool TryGetValue(int axis, int frame, out short value)
        {
            if (axis < 0 || axis >= MDLConstants.NumAxes)
            {
                throw new ArgumentOutOfRangeException(nameof(axis));
            }

            if (frame < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frame));
            }

            var list = Values[axis];

            var currentFrame = 0;

            for (var i = 0; i < list.Count; ++i)
            {
                var animValue = list[i];

                if (frame < currentFrame + animValue.Count)
                {
                    //Found the value range
                    value = animValue.Value;
                    return true;
                }

                currentFrame += animValue.Count;
            }

            value = default;

            return false;
        }
    }
}
