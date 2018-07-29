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
using System.Linq;

namespace SharpLife.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Denies any inputs that are not found in a given list
    /// </summary>
    public class StringListFilter : IVariableFilter
    {
        private readonly List<string> _strings;

        /// <summary>
        /// Creates a new string list filter
        /// </summary>
        /// <param name="strings"></param>
        public StringListFilter(IReadOnlyList<string> strings)
        {
            if (strings == null)
            {
                throw new ArgumentNullException(nameof(strings));
            }

            _strings = strings.ToList();
        }

        public bool Filter(ref string stringValue, ref float floatValue)
        {
            return _strings.Contains(stringValue);
        }
    }
}
