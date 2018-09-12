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

namespace SharpLife.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Clamps an input value to a numeric range
    /// </summary>
    public class MinMaxFilter : IVariableFilter
    {
        public float? Min { get; }

        public float? Max { get; }

        public bool DenyOutOfRangeValues { get; }

        /// <summary>
        /// Creates a new min-max filter
        /// You must specify at least one value
        /// </summary>
        /// <param name="min">Optional. Minimum value to clamp to</param>
        /// <param name="max">Optional. Maximum value to clamp to</param>
        /// <param name="denyOutOfRangeValues">If true, values out of range are denied instead of clamping them</param>
        public MinMaxFilter(float? min, float? max, bool denyOutOfRangeValues = false)
        {
            if (min == null && max == null)
            {
                throw new ArgumentException($"{nameof(MinMaxFilter)} has no purpose if both values are null", nameof(min));
            }

            if (min.HasValue && max.HasValue && max.Value <= min.Value)
            {
                throw new ArgumentOutOfRangeException("Minimum value must be less than maximum value");
            }

            Min = min;
            Max = max;
            DenyOutOfRangeValues = denyOutOfRangeValues;
        }

        public bool Filter(ref string stringValue, ref float floatValue)
        {
            var min = Min ?? floatValue;
            var max = Max ?? floatValue;

            //For unbounded clamps make sure the range is correct
            if (max < min)
            {
                var temp = min;
                min = max;
                max = temp;
            }

            var clampedValue = Math.Clamp(floatValue, min, max);

            if (clampedValue != floatValue)
            {
                if (DenyOutOfRangeValues)
                {
                    return false;
                }

                stringValue = CommandUtils.FloatToVariableString(floatValue);
                floatValue = clampedValue;
            }

            return true;
        }
    }
}
