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
using System.Globalization;
using System.Numerics;

namespace SharpLife.Game.Shared.Entities
{
    public static class KeyValueUtils
    {
        /// <summary>
        /// Parses an integer value the way Half-Life does
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ParseInt(string value)
        {
            int.TryParse(value, out var result);
            return result;
        }

        /// <summary>
        /// Parses a single precision floating point value the way Half-Life does
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float ParseFloat(string value)
        {
            float.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var result);
            return result;
        }

        /// <summary>
        /// Parses a single precision vector of 3 components the way Half-Life does
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector3 ParseVector3(string value)
        {
            var components = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return new Vector3(
                components.Length > 0 ? ParseFloat(components[0]) : 0,
                components.Length > 1 ? ParseFloat(components[1]) : 0,
                components.Length > 2 ? ParseFloat(components[2]) : 0
                );
        }

        /// <summary>
        /// Parses an enum value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T ParseEnum<T>(string value, T defaultValue)
            where T : struct, Enum
        {
            int.TryParse(value, out var result);

            return Enum.IsDefined(typeof(T), result) ? (T)(object)result : defaultValue;
        }
    }
}
