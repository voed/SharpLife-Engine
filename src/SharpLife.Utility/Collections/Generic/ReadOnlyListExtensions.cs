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

namespace SharpLife.Utility.Collections.Generic
{
    public static class ReadOnlyListExtensions
    {
        /// <summary>
        /// Identical in behavior to <see cref="List{T}.Find(Predicate{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T Find<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (var i = 0; i < list.Count; ++i)
            {
                if (predicate(list[i]))
                {
                    return list[i];
                }
            }

            return default;
        }
    }
}
