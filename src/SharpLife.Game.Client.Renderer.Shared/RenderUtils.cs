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

using SharpLife.Utility.Mathematics;
using System.Numerics;

namespace SharpLife.Game.Client.Renderer.Shared
{
    public static class RenderUtils
    {
        /// <summary>
        /// GoldSource identity matrix
        /// </summary>
        public static Matrix4x4 Identity => Matrix4x4.CreateRotationZ(MathUtils.ToRadians(-90)) * Matrix4x4.CreateRotationX(MathUtils.ToRadians(90)) * Matrix4x4.Identity;
    }
}
