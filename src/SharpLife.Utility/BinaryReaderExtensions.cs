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
using System.IO;
using System.Runtime.InteropServices;

namespace SharpLife.Utility
{
    public static class BinaryReaderExtensions
    {
        private static T InternalReadStructure<T>(BinaryReader reader)
        {
            return MarshalUtils.BytesToStructure<T>(reader.ReadBytes(Marshal.SizeOf<T>()));
        }

        public static T ReadStructure<T>(this BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return InternalReadStructure<T>(reader);
        }

        public static T[] ReadStructureArray<T>(this BinaryReader reader, int count)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var array = new T[count];

            for (var i = 0; i < count; ++i)
            {
                array[i] = InternalReadStructure<T>(reader);
            }

            return array;
        }
    }
}
