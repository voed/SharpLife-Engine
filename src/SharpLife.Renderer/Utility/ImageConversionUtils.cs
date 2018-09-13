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
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharpLife.Renderer.Utility
{
    /// <summary>
    /// Utilities to convert images to <see cref="Rgba32"/>
    /// </summary>
    public static class ImageConversionUtils
    {
        public const byte AlphaTestTransparentIndex = 255;
        public const byte IndexedAlphaColorIndex = 255;

        private const int ResampleRatio = 0x10000;

        public const int MinSizeExponent = 0;
        public static readonly int MaxSizeExponent = (8 * Marshal.SizeOf<int>()) - 1;

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

        private static Rgba32 InternalBoxFilter3x3(Span<Rgba32> pixels, int w, int h, int x, int y)
        {
            var numPixelsSampled = 0;

            int r = 0, g = 0, b = 0;

            for (var xIndex = 0; xIndex < 3; ++xIndex)
            {
                for (var yIndex = 0; yIndex < 3; ++yIndex)
                {
                    var column = (xIndex - 1) + x;
                    var row = (yIndex - 1) + y;

                    if (column >= 0 && column < w
                        && row >= 0 && row < h)
                    {
                        ref var pPixel = ref pixels[column + (w * row)];

                        if (pPixel.A != 0)
                        {
                            r += pPixel.R;
                            g += pPixel.G;
                            b += pPixel.B;
                            ++numPixelsSampled;
                        }
                    }
                }
            }

            if (numPixelsSampled == 0)
            {
                numPixelsSampled = 1;
            }

            return new Rgba32(
                (byte)(r / numPixelsSampled),
                (byte)(g / numPixelsSampled),
                (byte)(b / numPixelsSampled),
                pixels[x + (w * y)].A
            );
        }

        /// <summary>
        /// Averages the pixels in a 3x3 box around a pixel and returns the new value
        /// The alpha value is left unmodified
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="result"></param>
        public static Rgba32 BoxFilter3x3(Span<Rgba32> pixels, int w, int h, int x, int y)
        {
            if (w <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(w));
            }

            if (h <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(h));
            }

            if (x < 0 || x >= w)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y >= h)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            if (pixels.Length != w * h)
            {
                throw new ArgumentException("The given pixels span does not match the size of the given texture bounds");
            }

            return InternalBoxFilter3x3(pixels, w, h, x, y);
        }

        /// <summary>
        /// Averages the pixels in a 3x3 box around a pixel for all pixels
        /// Only pixels with alpha 0 are considered
        /// The alpha value is left unmodified
        /// The span is modified in place
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void BoxFilter3x3(Span<Rgba32> pixels, int w, int h)
        {
            if (w <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(w));
            }

            if (h <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(h));
            }

            if (pixels.Length != w * h)
            {
                throw new ArgumentException("The given pixels span does not match the size of the given texture bounds");
            }

            for (var i = 0; i < pixels.Length; ++i)
            {
                if (pixels[i].A == 0)
                {
                    pixels[i] = InternalBoxFilter3x3(
                        pixels,
                        w, h,
                        i % w, i / h);
                }
            }
        }

        /// <summary>
        /// Rescales and resamples a texture
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="inWidth"></param>
        /// <param name="inHeight"></param>
        /// <param name="outWidth"></param>
        /// <param name="outHeight"></param>
        /// <returns></returns>
        public static Rgba32[] ResampleTexture(Span<Rgba32> pixels, int inWidth, int inHeight, int outWidth, int outHeight)
        {
            if (inWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inWidth));
            }

            if (inHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inHeight));
            }

            if (outWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outWidth));
            }

            if (outHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outHeight));
            }

            if (pixels.Length != inWidth * inHeight)
            {
                throw new ArgumentException("The given pixels span does not match the size of the given texture bounds");
            }

            var p1 = new int[outWidth];
            var p2 = new int[outWidth];

            if (outWidth > 0)
            {
                var xScale = (inWidth * ResampleRatio) / outWidth;

                var row1Index = xScale / 4;
                var row2Index = 3 * (xScale / 4);

                for (int i = 0; i < outWidth; ++i)
                {
                    p1[i] = row1Index / ResampleRatio;
                    p2[i] = row2Index / ResampleRatio;

                    row1Index += xScale;
                    row2Index += xScale;
                }
            }

            var output = new Rgba32[outWidth * outHeight];

            var slice = new Span<Rgba32>(output);

            for (var i = 0; i < outHeight; ++i)
            {
                var inrow = pixels.Slice(inWidth * (int)((i + 0.25) * inHeight / (float)outHeight));
                var inrow2 = pixels.Slice(inWidth * (int)((i + 0.75) * inHeight / (float)outHeight));

                for (var j = 0; j < outWidth; ++j)
                {
                    var row1Index = p1[j];
                    var row2Index = p2[j];

                    slice[j].R = (byte)((inrow2[row1Index].R + inrow[row2Index].R + inrow[row1Index].R + inrow2[row2Index].R) / 4);
                    slice[j].G = (byte)((inrow2[row1Index].G + inrow[row2Index].G + inrow[row1Index].G + inrow2[row2Index].G) / 4);
                    slice[j].B = (byte)((inrow2[row1Index].B + inrow[row2Index].B + inrow[row1Index].B + inrow2[row2Index].B) / 4);
                    slice[j].A = (byte)((inrow2[row1Index].A + inrow[row2Index].A + inrow[row1Index].A + inrow2[row2Index].A) / 4);
                }

                slice = slice.Slice(outWidth);
            }

            return output;
        }

        private static int ComputeRoundedDownValue(int value, int roundDownExponent, int divisorExponent)
        {
            var scaledValue = MathUtils.NearestUpperPowerOf2(value);

            if ((roundDownExponent > 0) && (value < scaledValue) && (roundDownExponent == 1 || ((scaledValue - value) > (scaledValue >> roundDownExponent))))
            {
                scaledValue /= 2;
            }

            scaledValue >>= divisorExponent;

            return scaledValue;
        }

        /// <summary>
        /// Computes the new size of a texture
        /// </summary>
        /// <param name="inWidth">Original width</param>
        /// <param name="inHeight">Original height</param>
        /// <param name="roundDownExponent">Exponent used to round down the scaled size</param>
        /// <param name="divisorExponent">Exponent used to divide the scaled size further</param>
        /// <returns></returns>
        public static (int, int) ComputeScaledSize(int inWidth, int inHeight, int roundDownExponent, int divisorExponent)
        {
            if (inWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inWidth));
            }

            if (inHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inHeight));
            }

            if (roundDownExponent < MinSizeExponent || roundDownExponent > MaxSizeExponent)
            {
                throw new ArgumentOutOfRangeException(nameof(roundDownExponent));
            }

            if (divisorExponent < MinSizeExponent || divisorExponent > MaxSizeExponent)
            {
                throw new ArgumentOutOfRangeException(nameof(divisorExponent));
            }

            //Rescale image to nearest power of 2
            var scaledWidth = ComputeRoundedDownValue(inWidth, roundDownExponent, divisorExponent);
            var scaledHeight = ComputeRoundedDownValue(inHeight, roundDownExponent, divisorExponent);

            return (scaledWidth, scaledHeight);
        }
    }
}
