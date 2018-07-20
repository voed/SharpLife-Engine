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
using System.Text.RegularExpressions;

namespace SharpLife.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Extensions to make adding filters to convars easier
    /// </summary>
    public static class ConVarInfoFilterExtensions
    {
        public static ConVarInfo WithBooleanFilter(this ConVarInfo @this)
        {
            return @this.WithFilter(new BooleanFilter());
        }

        public static ConVarInfo WithNumberFilter(this ConVarInfo @this)
        {
            return @this.WithFilter(new NumberFilter());
        }

        public static ConVarInfo WithMinMaxFilter(this ConVarInfo @this, float? min, float? max, bool denyOutOfRangeValues = false)
        {
            return @this.WithFilter(new MinMaxFilter(min, max, denyOutOfRangeValues));
        }

        public static ConVarInfo WithRegexFilter(this ConVarInfo @this, Regex regex)
        {
            return @this.WithFilter(new RegexFilter(regex));
        }

        public static ConVarInfo WithRegexFilter(this ConVarInfo @this, string pattern)
        {
            return @this.WithFilter(new RegexFilter(new Regex(pattern)));
        }

        public static ConVarInfo WithStringListFilter(this ConVarInfo @this, IReadOnlyList<string> strings)
        {
            return @this.WithFilter(new StringListFilter(strings));
        }

        public static ConVarInfo WithStringListFilter(this ConVarInfo @this, params string[] strings)
        {
            return @this.WithFilter(new StringListFilter(strings));
        }

        public static ConVarInfo WithInvertedFilter(this ConVarInfo @this, IConVarFilter filter)
        {
            return @this.WithFilter(new InvertFilter(filter));
        }
    }
}
