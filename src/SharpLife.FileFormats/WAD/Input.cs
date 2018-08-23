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

using SharpLife.FileFormats.WAD.Disk;
using SharpLife.Utility;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.FileFormats.WAD
{
    public static class Input
    {
        private const int MipTexSize = 40;

        private static Header ReadHeader(BinaryReader reader)
        {
            var data = reader.ReadBytes(Marshal.SizeOf<Header>());

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var header = Marshal.PtrToStructure<Header>(handle.AddrOfPinnedObject());
            handle.Free();

            if (!Enum.IsDefined(typeof(WADVersion), header.identification))
            {
                throw new InvalidWADVersionException(header.identification);
            }

            header.infotableofs = EndianConverter.Little(header.infotableofs);
            header.numlumps = EndianConverter.Little(header.numlumps);

            return header;
        }

        private static List<LumpInfo> ReadLumps(BinaryReader reader, ref Header header)
        {
            reader.BaseStream.Position = header.infotableofs;

            var lumps = new List<LumpInfo>(header.numlumps);

            foreach (var i in Enumerable.Range(0, header.numlumps))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<LumpInfo>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var lump = Marshal.PtrToStructure<LumpInfo>(handle.AddrOfPinnedObject());
                handle.Free();

                lump.filepos = EndianConverter.Little(lump.filepos);
                lump.size = EndianConverter.Little(lump.size);

                lumps.Add(lump);
            }

            return lumps;
        }

        public static MipTexture ReadMipTexture(BinaryReader reader, int fileOffset)
        {
            var startPos = reader.BaseStream.Position = fileOffset;

            //Read fixed size array from file
            var name = reader.ReadBytes(Constants.MaxTextureNameLength + 1);
            var width = reader.ReadUInt32();
            var height = reader.ReadUInt32();

            var offsets = new uint[Constants.NumMipLevels];

            foreach (var offset in Enumerable.Range(0, Constants.NumMipLevels))
            {
                offsets[offset] = reader.ReadUInt32();
            }

            var utf8Name = Encoding.UTF8.GetString(name);

            //Remove null terminator and any other characters following the name
            utf8Name = utf8Name.Substring(0, Math.Max(0, utf8Name.IndexOf('\0')));

            var texture = new MipTexture
            {
                Name = utf8Name,
                Width = width,
                Height = height,
            };

            //Maps always have miptex entries, but they aren't necessarily embedded textures
            if (offsets[0] != 0)
            {
                foreach (var offset in Enumerable.Range(0, Constants.NumMipLevels))
                {
                    //The offsets are relative to the start of the mipmap structure
                    reader.BaseStream.Position = startPos + offsets[offset];

                    //Each successive mipmap is half the size of the previous mipmap
                    var divider = Math.Pow(2, offset);
                    texture.Data[offset] = reader.ReadBytes((int)(width / divider * height / divider));
                }

                //Read the palette
                //Located directly after the pixel data
                //This calculation came from Half-Life utility code, it determines the total size of all of the pixel data
                var pixelDataSize = width * height / 64 * 85;

                reader.BaseStream.Position = startPos + MipTexSize + pixelDataSize;

                var paletteSize = reader.ReadInt16();

                if (paletteSize != Constants.NumPaletteColors)
                {
                    throw new ArgumentException("Invalid miptex");
                }

                foreach (var i in Enumerable.Range(0, Constants.NumPaletteColors))
                {
                    var paletteData = reader.ReadBytes(Constants.NumPaletteComponents * Constants.PaletteComponentSizeInBytes);

                    GCHandle handle = GCHandle.Alloc(paletteData, GCHandleType.Pinned);
                    texture.Palette[i] = Marshal.PtrToStructure<Rgb24>(handle.AddrOfPinnedObject());
                    handle.Free();
                }
            }

            return texture;
        }

        private static List<MipTexture> ReadMipTextures(BinaryReader reader, IReadOnlyList<LumpInfo> lumps)
        {
            var textures = new List<MipTexture>(lumps.Count);

            foreach (var lump in lumps)
            {
                textures.Add(ReadMipTexture(reader, lump.filepos));
            }

            return textures;
        }

        public static WADFile ReadWADFile(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var header = ReadHeader(reader);

            var lumps = ReadLumps(reader, ref header);

            var textures = ReadMipTextures(reader, lumps);

            var wadFile = new WADFile
            {
                Version = (WADVersion)header.identification,
                MipTextures = textures
            };

            return wadFile;
        }

        public static WADFile ReadWADFile(Stream stream)
        {
            return ReadWADFile(new BinaryReader(stream));
        }
    }
}
