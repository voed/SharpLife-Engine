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

using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;

namespace SharpLife.Renderer
{
    /// <summary>
    /// Immutable container for indexed 256 color images
    /// </summary>
    public sealed class IndexedColor256Texture
    {
        public Rgb24[] Palette { get; }

        public byte[] Pixels { get; }

        public int Width { get; }

        public int Height { get; }

        public IndexedColor256Texture(Rgb24[] palette, byte[] pixels, int width, int height)
        {
            Palette = (palette ?? throw new ArgumentNullException(nameof(palette))).ToArray();

            if (Palette.Length != IndexPaletteConstants.NumPaletteColors)
            {
                throw new ArgumentOutOfRangeException(nameof(palette),
                    $"The given palette has an invalid number of colors ({palette.Length} instead of {IndexPaletteConstants.NumPaletteColors})");
            }

            Pixels = (pixels ?? throw new ArgumentNullException(nameof(pixels))).ToArray();

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            Width = width;

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Height = height;

            if (Pixels.Length != (width * height))
            {
                throw new ArgumentOutOfRangeException(nameof(pixels), $"The given pixel data does not have the given width and height");
            }
        }
    }
}
