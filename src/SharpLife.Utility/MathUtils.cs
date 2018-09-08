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

namespace SharpLife.Utility
{
    public static class MathUtils
    {
        public static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        public static float ToRadians(float degrees)
        {
            return (float)ToRadians((double)degrees);
        }

        public static double ToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        public static float ToDegrees(float degrees)
        {
            return (float)ToDegrees((double)degrees);
        }
    }
}
