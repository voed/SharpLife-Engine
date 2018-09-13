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

using SixLabors.ImageSharp.PixelFormats;

namespace SharpLife.FileFormats.WAD
{
    public class MipTexture
    {
        public string Name { get; set; }

        public uint Width { get; set; }
        public uint Height { get; set; }

        /// <summary>
        /// Texture data stored as raw bytes
        /// </summary>
        public byte[][] Data { get; } = new byte[WADConstants.NumMipLevels][]; // four mip maps stored

        public Rgb24[] Palette { get; } = new Rgb24[WADConstants.NumPaletteColors];
    }
}
