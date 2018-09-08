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

using SharpLife.FileFormats.WAD;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace SharpLife.FileFormats.SPR
{
    public class SpriteFile
    {
        public SpriteType Type { get; set; }

        public SpriteTextureFormat TextureFormat { get; set; }

        public float BoundingRadius { get; set; }

        public int MaximumWidth { get; set; }

        public int MaximumHeight { get; set; }

        public Rgb24[] Palette { get; }

        public List<SpriteFrame> Frames { get; set; }

        public SpriteFile()
        {
            //TODO: move palette constants to utility
            Palette = new Rgb24[Constants.NumPaletteColors];
        }

        public SpriteFile(Rgb24[] palette)
        {
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));

            if (palette.Length != Constants.NumPaletteColors)
            {
                throw new ArgumentException("Palette size is invalid");
            }
        }
    }
}
