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

using SharpLife.Utility;
using System;
using System.IO;
using System.Text;

namespace SharpLife.FileFormats.MDL
{
    public sealed class StudioLoader
    {
        private readonly BinaryReader _reader;

        public StudioLoader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public StudioLoader(Stream stream, bool leaveOpen)
            : this(new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, leaveOpen))
        {
        }

        public StudioLoader(Stream stream)
            : this(stream, false)
        {
        }

        public StudioLoader(string fileName)
            : this(File.OpenRead(fileName))
        {
        }

        private static int ReadIdentifier(BinaryReader reader)
        {
            return EndianConverter.Little(reader.ReadInt32());
        }

        public static bool IsStudioFile(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var originalPosition = reader.BaseStream.Position;

            try
            {
                return MDLConstants.HeaderIdentifier == ReadIdentifier(reader);
            }
            finally
            {
                reader.BaseStream.Position = originalPosition;
            }
        }

        public StudioFile ReadStudioFile()
        {
            //TODO: implement
            return new StudioFile();
        }

        public uint ComputeCRC()
        {
            //TODO: implement
            return 0;
        }
    }
}
