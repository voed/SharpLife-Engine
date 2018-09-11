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
    /// Extensions to make adding filters to variables easier
    /// </summary>
    public static class VariableInfoFilterExtensions
    {
        public static VariableInfo WithBooleanFilter(this VariableInfo @this)
        {
            return @this.WithFilter(new BooleanFilter());
        }

        /// <summary>
        /// <see cref="NumberFilter(bool)"/>
        /// </summary>
        /// <param name="this"></param>
        /// <param name="integerOnly"></param>
        /// <returns></returns>
        public static VariableInfo WithNumberFilter(this VariableInfo @this, bool integerOnly = false)
        {
            return @this.WithFilter(new NumberFilter(integerOnly));
        }

        /// <summary>
        /// <see cref="NumberSignFilter(bool)"/>
        /// </summary>
        /// <param name="this"></param>
        /// <param name="positive"></param>
        /// <returns></returns>
        public static VariableInfo WithNumberSignFilter(this VariableInfo @this, bool positive)
        {
            return @this.WithFilter(new NumberSignFilter(positive));
        }

        public static VariableInfo WithMinMaxFilter(this VariableInfo @this, float? min, float? max, bool denyOutOfRangeValues = false)
        {
            return @this.WithFilter(new MinMaxFilter(min, max, denyOutOfRangeValues));
        }

        public static VariableInfo WithRegexFilter(this VariableInfo @this, Regex regex)
        {
            return @this.WithFilter(new RegexFilter(regex));
        }

        public static VariableInfo WithRegexFilter(this VariableInfo @this, string pattern)
        {
            return @this.WithFilter(new RegexFilter(new Regex(pattern)));
        }

        public static VariableInfo WithStringListFilter(this VariableInfo @this, IReadOnlyList<string> strings)
        {
            return @this.WithFilter(new StringListFilter(strings));
        }

        public static VariableInfo WithStringListFilter(this VariableInfo @this, params string[] strings)
        {
            return @this.WithFilter(new StringListFilter(strings));
        }

        public static VariableInfo WithInvertedFilter(this VariableInfo @this, IVariableFilter filter)
        {
            return @this.WithFilter(new InvertFilter(filter));
        }

        public static VariableInfo WithDelegateFilter(this VariableInfo @this, DelegateFilter.FilterDelegate @delegate)
        {
            return @this.WithFilter(new DelegateFilter(@delegate));
        }

        public static VariableInfo WithPrintableCharactersFilter(this VariableInfo @this, string emptyValue = "")
        {
            return @this.WithFilter(new UnprintableCharactersFilter(emptyValue));
        }

        public static VariableInfo WithWhitespaceFilter(this VariableInfo @this)
        {
            return @this.WithFilter(new StripWhitespaceFilter());
        }
    }
}
