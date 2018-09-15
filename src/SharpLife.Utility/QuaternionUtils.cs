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
using System.Numerics;

namespace SharpLife.Utility
{
    public static class QuaternionUtils
    {
        /// <summary>
        /// Note: this differs from Quaternion.CreateFromYawPitchRoll, they are not equivalent
        /// </summary>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Quaternion AngleToQuaternion(in Vector3 angles)
        {
            // FIXME: rescale the inputs to 1/2 angle
            var angle = angles.Z * 0.5f;
            var sy = (float)Math.Sin(angle);
            var cy = (float)Math.Cos(angle);
            angle = angles.Y * 0.5f;
            var sp = (float)Math.Sin(angle);
            var cp = (float)Math.Cos(angle);
            angle = angles.X * 0.5f;
            var sr = (float)Math.Sin(angle);
            var cr = (float)Math.Cos(angle);

            Quaternion quaternion;

            quaternion.X = (sr * cp * cy) - (cr * sp * sy);
            quaternion.Y = (cr * sp * cy) + (sr * cp * sy);
            quaternion.Z = (cr * cp * sy) - (sr * sp * cy);
            quaternion.W = (cr * cp * cy) + (sr * sp * sy);

            return quaternion;
        }
    }
}
