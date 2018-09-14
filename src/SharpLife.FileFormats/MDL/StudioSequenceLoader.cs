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

using SharpLife.FileFormats.MDL.Disk;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpLife.FileFormats.MDL
{
    public sealed class StudioSequenceLoader
    {
        private readonly BinaryReader _reader;

        public StudioSequenceLoader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public StudioSequenceLoader(Stream stream, bool leaveOpen)
            : this(new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, leaveOpen))
        {
        }

        public StudioSequenceLoader(Stream stream)
            : this(stream, false)
        {
        }

        public StudioSequenceLoader(string fileName)
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
                return MDLConstants.SequenceHeaderIdentifier == ReadIdentifier(reader);
            }
            finally
            {
                reader.BaseStream.Position = originalPosition;
            }
        }

        private SequenceHeader ReadHeader()
        {
            var position = _reader.BaseStream.Position;

            var identifier = ReadIdentifier(_reader);

            //Verify that we can load this MDL file
            //TODO: maybe pass the invalid value along with the exception
            if (identifier != MDLConstants.SequenceHeaderIdentifier)
            {
                throw new InvalidMDLIdException();
            }

            var version = EndianConverter.Little(_reader.ReadInt32());

            if (!Enum.IsDefined(typeof(MDLVersion), version))
            {
                throw new InvalidMDLVersionException();
            }

            _reader.BaseStream.Position = position;

            var header = _reader.ReadStructure<SequenceHeader>();

            header.Id = EndianConverter.Little(header.Id);
            header.Version = EndianConverter.Little(header.Version);
            header.Length = EndianConverter.Little(header.Length);

            return header;
        }

        public void ReadAnimations(StudioFile studioFile, int sequenceGroup, IReadOnlyList<Disk.SequenceDescriptor> rawSequences)
        {
            if (studioFile == null)
            {
                throw new ArgumentNullException(nameof(studioFile));
            }

            var header = ReadHeader();

            for (var i = 0; i < rawSequences.Count; ++i)
            {
                var rawSequence = rawSequences[i];

                if (rawSequence.SeqGroup == sequenceGroup)
                {
                    studioFile.Sequences[i].AnimationBlends = StudioIOUtils.ReadAnimationBlends(
                        _reader, studioFile.Bones.Count, 0, rawSequence, studioFile.Sequences[i]);
                }
            }
        }
    }
}
