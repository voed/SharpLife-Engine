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
using System;
using System.Linq;

namespace SharpLife.Renderer.Utility
{
    /// <summary>
    /// Utilities to convert images to <see cref="Rgba32"/>
    /// </summary>
    public static class ImageConversionUtils
    {
        public const byte AlphaTestTransparentIndex = 255;
        public const byte IndexedAlphaColorIndex = 255;

        /// <summary>
        /// Convert an indexed 256 color image to an Rgba32 image
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="bitmap"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Rgba32[] ConvertNormal(Rgb24[] palette, byte[] bitmap, int width, int height)
        {
            var pixels = new Rgba32[width * height];

            foreach (var i in Enumerable.Range(0, width * height))
            {
                palette[bitmap[i]].ToRgba32(ref pixels[i]);
            }

            return pixels;
        }

        /// <summary>
        /// Alpha test: convert all indices to their color except index 255, which is converted as fully transparent
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="bitmap"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Rgba32[] ConvertAlphaTest(Rgb24[] palette, byte[] bitmap, int width, int height)
        {
            var pixels = new Rgba32[width * height];

            foreach (var i in Enumerable.Range(0, width * height))
            {
                var index = bitmap[i];

                if (index != AlphaTestTransparentIndex)
                {
                    palette[index].ToRgba32(ref pixels[i]);
                }
                else
                {
                    //RGB values default to 0 in array initializer
                    pixels[i].A = 0;
                }
            }

            return pixels;
        }

        /// <summary>
        /// Indexed alpha: grayscale indexed 255 color image with single color gradient
        /// Transparency is determined by index value
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="bitmap"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Rgba32[] ConvertIndexedAlpha(Rgb24[] palette, byte[] bitmap, int width, int height)
        {
            var pixels = new Rgba32[width * height];

            ref var color = ref palette[IndexedAlphaColorIndex];

            foreach (var i in Enumerable.Range(0, width * height))
            {
                pixels[i].Rgb = color;
                pixels[i].A = bitmap[i];
            }

            return pixels;
        }

        public static Rgba32[] ConvertIndexedToRgba32(Rgb24[] palette, byte[] bitmap, int width, int height, TextureFormat format)
        {
            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (bitmap.Length != (width * height))
            {
                throw new ArgumentException("Bitmap does not have correct dimensions");
            }

            switch (format)
            {
                case TextureFormat.Normal:
                    return ConvertNormal(palette, bitmap, width, height);
                case TextureFormat.AlphaTest:
                    return ConvertAlphaTest(palette, bitmap, width, height);
                case TextureFormat.IndexAlpha:
                    return ConvertIndexedAlpha(palette, bitmap, width, height);

                default: throw new ArgumentException("Invalid texture format", nameof(format));
            }
        }
    }
}
