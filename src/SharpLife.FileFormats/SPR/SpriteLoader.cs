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
using SharpLife.FileFormats.SPR.Disk;
using SharpLife.FileFormats.WAD;
using SharpLife.Utility;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.FileFormats.SPR
{
    public sealed class SpriteLoader
    {
        private readonly BinaryReader _reader;

        private readonly long _startPosition;

        public SpriteLoader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _startPosition = _reader.BaseStream.Position;
        }

        public SpriteLoader(Stream stream, bool leaveOpen)
            : this(new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, leaveOpen))
        {
        }

        public SpriteLoader(Stream stream)
            : this(stream, false)
        {
        }

        public SpriteLoader(string fileName)
            : this(File.OpenRead(fileName))
        {
        }

        private static int ReadIdentifier(BinaryReader reader)
        {
            return EndianConverter.Little(reader.ReadInt32());
        }

        public static bool IsSpriteFile(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var originalPosition = reader.BaseStream.Position;

            try
            {
                return SPRConstants.Identifier == ReadIdentifier(reader);
            }
            finally
            {
                reader.BaseStream.Position = originalPosition;
            }
        }

        private SpriteHeader ReadHeader()
        {
            {
                var originalPosition = _reader.BaseStream.Position;

                var identifier = EndianConverter.Little(ReadIdentifier(_reader));

                if (identifier != SPRConstants.Identifier)
                {
                    throw new FileLoadFailureException("Sprite file does not match expected identifier");
                }

                var version = EndianConverter.Little(_reader.ReadInt32());

                //Verify that we can load this SPR file
                if (!Enum.IsDefined(typeof(SpriteVersion), version))
                {
                    throw new InvalidSPRVersionException(version);
                }

                _reader.BaseStream.Position = originalPosition;
            }

            var data = _reader.ReadBytes(Marshal.SizeOf<SpriteHeader>());

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var header = Marshal.PtrToStructure<SpriteHeader>(handle.AddrOfPinnedObject());
            handle.Free();

            header.Identifier = EndianConverter.Little(header.Identifier);
            header.Version = EndianConverter.Little(header.Version);
            header.Type = EndianConverter.Little(header.Type);
            header.TextureFormat = EndianConverter.Little(header.TextureFormat);
            header.BoundingRadius = EndianConverter.Little(header.BoundingRadius);
            header.Width = EndianConverter.Little(header.Width);
            header.Height = EndianConverter.Little(header.Height);
            header.NumFrames = EndianConverter.Little(header.NumFrames);
            header.BeamLength = EndianConverter.Little(header.BeamLength);
            header.SyncType = EndianConverter.Little(header.SyncType);

            return header;
        }

        private unsafe SpriteFrame ReadFrame()
        {
            var frameData = _reader.ReadBytes(Marshal.SizeOf<Disk.SpriteFrame>());

            var handle = GCHandle.Alloc(frameData, GCHandleType.Pinned);
            var frameDataStruct = Marshal.PtrToStructure<Disk.SpriteFrame>(handle.AddrOfPinnedObject());
            handle.Free();

            frameDataStruct.Origin[0] = EndianConverter.Little(frameDataStruct.Origin[0]);
            frameDataStruct.Origin[1] = EndianConverter.Little(frameDataStruct.Origin[1]);

            frameDataStruct.Width = EndianConverter.Little(frameDataStruct.Width);
            frameDataStruct.Height = EndianConverter.Little(frameDataStruct.Height);

            var textureData = _reader.ReadBytes(frameDataStruct.Width * frameDataStruct.Height);

            return new SpriteFrame
            {
                //Y origin has to be inverted (sprgen inverts it to produce coordinates better fit for the original engine)
                Area = new Rectangle(
                    frameDataStruct.Origin[0],
                    -frameDataStruct.Origin[1],
                    frameDataStruct.Width,
                    frameDataStruct.Height
                    ),
                TextureData = textureData
            };
        }

        //Reads and discards a group (not supported)
        private void ReadGroup()
        {
            var numFrames = EndianConverter.Little(_reader.ReadInt32()); //Group frame count

            //Interval array
            for (var i = 0; i < numFrames; ++i)
            {
                _reader.ReadSingle();
            }

            //Frame array
            for (var i = 0; i < numFrames; ++i)
            {
                ReadFrame();
            }
        }

        public SpriteFile ReadSpriteFile()
        {
            var header = ReadHeader();

            //Read the palette
            var colorCount = EndianConverter.Little(_reader.ReadInt16());
            var paletteData = new byte[WADConstants.PaletteSizeInBytes];
            _reader.Read(paletteData, 0, colorCount * WADConstants.NumPaletteComponents * WADConstants.PaletteComponentSizeInBytes);

            //Convert the palette to an Rgb24 array
            var palette = new Rgb24[WADConstants.NumPaletteColors];

            for (var i = 0; i < WADConstants.NumPaletteColors; ++i)
            {
                palette[i] = new Rgb24(
                    paletteData[i * WADConstants.NumPaletteComponents],
                    paletteData[(i * WADConstants.NumPaletteComponents) + 1],
                    paletteData[(i * WADConstants.NumPaletteComponents) + 2]
                    );
            }

            var frames = new List<SpriteFrame>();

            for (var i = 0; i < header.NumFrames; ++i)
            {
                var type = (SpriteFrameType)EndianConverter.Little(_reader.ReadInt32());

                if (type == SpriteFrameType.Single)
                {
                    frames.Add(ReadFrame());
                }
                else
                {
                    //Groups are not supported, read and discard
                    ReadGroup();
                    //TODO: to match original behavior, maybe mark the last frame as covering 2 frames worth of time?
                }
            }

            return new SpriteFile(palette)
            {
                Type = (SpriteType)header.Type,
                TextureFormat = (SpriteTextureFormat)header.TextureFormat,
                BoundingRadius = header.BoundingRadius,
                MaximumWidth = header.Width,
                MaximumHeight = header.Height,
                Frames = frames
            };
        }

        public uint ComputeCRC()
        {
            var currentPosition = _reader.BaseStream.Position;

            _reader.BaseStream.Position = _startPosition;

            try
            {
                uint crc = 0;

                var buffer = new byte[1024];

                var bytesLeft = _reader.BaseStream.Length;

                while (bytesLeft > 0)
                {
                    var bytesToRead = bytesLeft < buffer.Length ? (int)bytesLeft : buffer.Length;

                    var bytesRead = _reader.Read(buffer, 0, bytesToRead);

                    crc = Crc32Algorithm.Append(crc, buffer, 0, bytesToRead);

                    bytesLeft -= bytesToRead;
                }

                return crc;
            }
            finally
            {
                _reader.BaseStream.Position = currentPosition;
            }
        }
    }
}
