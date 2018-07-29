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
using System.Text;

namespace SharpLife.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Filters string input and removes unprintable characters
    /// </summary>
    public class UnprintableCharactersFilter : IVariableFilter
    {
        private readonly string _emptyValue;

        /// <summary>
        /// Creates a new printable characters filter
        /// </summary>
        /// <param name="emptyValue">Optional. If provided, if the resulting string is empty, this value will be used instead</param>
        public UnprintableCharactersFilter(string emptyValue = "")
        {
            _emptyValue = emptyValue ?? throw new ArgumentNullException(nameof(emptyValue));
        }

        public bool Filter(ref string stringValue, ref float floatValue)
        {
            var builder = new StringBuilder();

            foreach (var c in stringValue)
            {
                if (!char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    builder.Append(c);
                }
            }

            stringValue = builder.ToString();

            if (stringValue.Length == 0)
            {
                stringValue = _emptyValue;
            }

            return true;
        }
    }
}
