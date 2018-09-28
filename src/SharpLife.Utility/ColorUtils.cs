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

using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace SharpLife.Utility
{
    public static class ColorUtils
    {
        public static Vector3 ToVector3(in Rgb24 color)
        {
            return new Vector3(color.R / (float)byte.MaxValue, color.G / (float)byte.MaxValue, color.B / (float)byte.MaxValue);
        }

        public static Rgb24 ToRgb24(in Vector3 color)
        {
            return new Rgb24((byte)(color.X * byte.MaxValue), (byte)(color.Y * byte.MaxValue), (byte)(color.Z * byte.MaxValue));
        }
    }
}
