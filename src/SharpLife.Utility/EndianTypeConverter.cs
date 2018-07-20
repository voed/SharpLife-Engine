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

namespace SharpLife.Utility
{
    public static class EndianTypeConverter
    {
        public static Vector3 Little(in Vector3 value)
        {
            return new Vector3(
                EndianConverter.Little(value.X),
                EndianConverter.Little(value.Y),
                EndianConverter.Little(value.Z)
                );
        }

        public static Vector3 Big(in Vector3 value)
        {
            return new Vector3(
                EndianConverter.Big(value.X),
                EndianConverter.Big(value.Y),
                EndianConverter.Big(value.Z)
                );
        }

        public static Vector4 Little(in Vector4 value)
        {
            return new Vector4(
                EndianConverter.Little(value.X),
                EndianConverter.Little(value.Y),
                EndianConverter.Little(value.Z),
                EndianConverter.Little(value.W)
                );
        }

        public static Vector4 Big(in Vector4 value)
        {
            return new Vector4(
                EndianConverter.Big(value.X),
                EndianConverter.Big(value.Y),
                EndianConverter.Big(value.Z),
                EndianConverter.Big(value.W)
                );
        }
    }
}
