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

namespace SharpLife.Utility.Mathematics
{
    public static class VectorUtils
    {
        public const float EqualEpsilon = 0.001f;

        public static Vector3 ToRadians(in Vector3 anglesInDegrees)
        {
            return new Vector3(
                MathUtils.ToRadians(anglesInDegrees.X),
                MathUtils.ToRadians(anglesInDegrees.Y),
                MathUtils.ToRadians(anglesInDegrees.Z)
                );
        }

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
        /// Converts an angle into vectors representing the forward, right and up directions
        /// </summary>
        /// <param name="angles"></param>
        /// <param name="forward"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        public static void AngleToVectors(in Vector3 angles, out Vector3 forward, out Vector3 right, out Vector3 up)
        {
            var angle = angles.Y * (Math.PI * 2 / 360);
            var sy = (float)Math.Sin(angle);
            var cy = (float)Math.Cos(angle);
            angle = angles.X * (Math.PI * 2 / 360);
            var sp = (float)Math.Sin(angle);
            var cp = (float)Math.Cos(angle);
            angle = angles.Z * (Math.PI * 2 / 360);
            var sr = (float)Math.Sin(angle);
            var cr = (float)Math.Cos(angle);

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

        public static void AngleToVectors(in Vector3 angles, out DirectionalVectors vectors)
        {
            AngleToVectors(angles, out vectors.Forward, out vectors.Right, out vectors.Up);
        }

        public static float AngleBetweenVectors(in Vector3 v1, in Vector3 v2)
        {
            var l1 = v1.Length();
            var l2 = v2.Length();

            if (l1 == 0 || l2 == 0)
            {
                return 0.0f;
            }

            var angle = Math.Acos(Vector3.Dot(v1, v2)) / (l1 * l2);

            return (float)MathUtils.ToDegrees(angle);
        }

        /// <summary>
        /// Compares two vectors and returns if they are equal, to within <see cref="EqualEpsilon"/> units difference
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool VectorsEqual(in Vector3 lhs, in Vector3 rhs)
        {
            return (Math.Abs(lhs.X - rhs.X) <= EqualEpsilon)
                && (Math.Abs(lhs.Y - rhs.Y) <= EqualEpsilon)
                && (Math.Abs(lhs.Z - rhs.Z) <= EqualEpsilon);
        }
    }
}
