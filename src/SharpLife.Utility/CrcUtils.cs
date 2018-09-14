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

using Force.Crc32;
using System;
using System.IO;

namespace SharpLife.Utility
{
    public static class CrcUtils
    {
        public static uint ComputeCRC(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            uint crc = 0;

            var buffer = new byte[1024];

            var bytesLeft = reader.BaseStream.Length;

            while (bytesLeft > 0)
            {
                var bytesToRead = bytesLeft < buffer.Length ? (int)bytesLeft : buffer.Length;

                var bytesRead = reader.Read(buffer, 0, bytesToRead);

                crc = Crc32Algorithm.Append(crc, buffer, 0, bytesToRead);

                bytesLeft -= bytesToRead;
            }

            return crc;
        }

        /// <summary>
        /// Computes the CRC and resets the read position back to its original value
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="startPosition"></param>
        /// <returns></returns>
        public static uint ComputeCRC(BinaryReader reader, long startPosition)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var currentPosition = reader.BaseStream.Position;

            reader.BaseStream.Position = startPosition;

            try
            {
                return ComputeCRC(reader);
            }
            finally
            {
                reader.BaseStream.Position = currentPosition;
            }
        }
    }
}
