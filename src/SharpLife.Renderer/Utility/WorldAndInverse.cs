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

using System.Numerics;
using Veldrid.Utilities;

namespace SharpLife.Renderer.Utility
{
    public struct WorldAndInverse
    {
        public Matrix4x4 World;
        public Matrix4x4 InverseWorld;

        /// <summary>
        /// Creates world and inverse data from settings
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="angles"></param>
        /// <param name="scale"></param>
        public WorldAndInverse(in Vector3 origin, in Vector3 angles, in Vector3 scale)
        {
            World = Matrix4x4.CreateScale(scale)
                * CreateRotationMatrix(angles)
                * Matrix4x4.CreateTranslation(origin);

            InverseWorld = VdUtilities.CalculateInverseTranspose(ref World);
        }

        public static Matrix4x4 CreateRotationMatrix(in Vector3 angles)
        {
            //TODO: verify that the angles are correctly used here
            return Matrix4x4.CreateFromYawPitchRoll(angles.X, angles.Z, angles.Y);
        }
    }
}
