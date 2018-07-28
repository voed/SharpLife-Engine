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
    public static class VectorUtils
    {
        /// <summary>
        /// Converts a directional vector to angles
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 VectorToAngles(in Vector3 vector)
        {
            float tmp, yaw, pitch;

            if (vector.Y == 0 && vector.X == 0)
            {
                yaw = 0;
                if (vector.Z > 0)
                {
                    pitch = 90;
                }
                else
                {
                    pitch = 270;
                }
            }
            else
            {
                yaw = ((float)(Math.Atan2(vector.Y, vector.X) * 180 / Math.PI));
                if (yaw < 0)
                {
                    yaw += 360;
                }

                tmp = (float)Math.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
                pitch = ((float)(Math.Atan2(vector.Z, tmp) * 180 / Math.PI));
                if (pitch < 0)
                {
                    pitch += 360;
                }
            }

            return new Vector3(pitch, yaw, 0);
        }

        /// <summary>
        /// Converts an angle to directional vectors
        /// </summary>
        /// <param name="angles"></param>
        /// <param name="forward"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        public static void AngleToVectors(in Vector3 angles, out Vector3 forward, out Vector3 right, out Vector3 up)
        {
            float angle = (float)(angles.Y * (Math.PI * 2 / 360));
            float sy = (float)Math.Sin(angle);
            float cy = (float)Math.Cos(angle);
            angle = (float)(angles.X * (Math.PI * 2 / 360));
            float sp = (float)Math.Sin(angle);
            float cp = (float)Math.Cos(angle);
            angle = (float)(angles.Z * (Math.PI * 2 / 360));
            float sr = (float)Math.Sin(angle);
            float cr = (float)Math.Cos(angle);

            forward = new Vector3(
                cp * cy,
                cp * sy,
                -sp
            );

            right = new Vector3(
                (-1 * sr * sp * cy) + (-1 * cr * -sy),
                (-1 * sr * sp * sy) + (-1 * cr * cy),
                -1 * sr * cp
            );

            up = new Vector3(
                (cr * sp * cy) + (-sr * -sy),
                (cr * sp * sy) + (-sr * cy),
                cr * cp
            );
        }
    }
}
