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

namespace SharpLife.Utility
{
    public static class ImGuiUtils
    {
        public const int Color32RShift = 0;
        public const int Color32GShift = 8;
        public const int Color32BShift = 16;
        public const int Color32AShift = 24;

        /// <summary>
        /// Create an ImGui Color32 value
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static uint Color32(byte r, byte g, byte b, byte a)
        {
            return ((uint)(a) << Color32AShift) | ((uint)(b) << Color32BShift) | ((uint)(g) << Color32GShift) | ((uint)(r) << Color32RShift);
        }
    }
}
