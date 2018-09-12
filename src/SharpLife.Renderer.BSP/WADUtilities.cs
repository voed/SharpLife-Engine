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
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;
using Veldrid.ImageSharp;

namespace SharpLife.Renderer.BSP
{
    public static class WADUtilities
    {
        /// <summary>
        /// Uploads all of the given textures, adding them to the cache
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="factory"></param>
        /// <param name="cache"></param>
        /// <param name="textures"></param>
        public static void UploadTextures(GraphicsDevice gd, ResourceFactory factory, ResourceCache cache, IReadOnlyList<MipTexture> textures)
        {
            foreach (var texture in textures)
            {
                //Convert the image data to an ImageSharp image
                var data = texture.Data[0];

                if (data == null)
                {
                    throw new InvalidOperationException($"Texture \"{texture.Name}\" has no pixel data");
                }

                var palette = texture.Palette.ToArray();

                var width = (int)texture.Width;
                var height = (int)texture.Height;

                var textureFormat = texture.Name.StartsWith('{') ? TextureFormat.AlphaTest : TextureFormat.Normal;

                var pixels = ImageConversionUtils.ConvertIndexedToRgba32(palette, texture.Data[0], width, height, textureFormat);

                //Alpha tested textures have their fully transparent pixels modified so samplers won't sample the color used and blend it
                //This stops the color from bleeding through
                if (textureFormat == TextureFormat.AlphaTest
                    || textureFormat == TextureFormat.IndexAlpha)
                {
                    ImageConversionUtils.BoxFilter3x3(pixels, width, height);
                }

                //Rescale image to nearest power of 2
                //TODO: use cvar for round down & division
                (var scaledWidth, var scaledHeight) = ImageConversionUtils.ComputeScaledSize(width, height, 3, 0);

                var scaledPixels = ImageConversionUtils.ResampleTexture(new Span<Rgba32>(pixels), width, height, scaledWidth, scaledHeight);

                var image = Image.LoadPixelData(scaledPixels, scaledWidth, scaledHeight);

                cache.AddTexture2D(gd, factory, new ImageSharpTexture(image, true), texture.Name);
            }
        }
    }
}
