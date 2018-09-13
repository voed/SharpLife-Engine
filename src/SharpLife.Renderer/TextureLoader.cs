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

using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Veldrid;
using Veldrid.ImageSharp;

namespace SharpLife.Renderer
{
    public class TextureLoader
    {
        private const int DefaultRoundDown = 3;
        private const int DefaultPicMip = 0;

        private readonly IVariable _roundDown;

        private readonly IVariable _picMip;

        public TextureLoader(ICommandContext commandContext)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            _roundDown = commandContext.RegisterVariable(
                new VariableInfo("mat_round_down")
                .WithValue(DefaultRoundDown)
                .WithHelpInfo("If not 0, this is used to round down texture sizes")
                .WithNumberFilter(true)
                .WithMinMaxFilter(ImageConversionUtils.MinSizeExponent, ImageConversionUtils.MaxSizeExponent, true));

            _picMip = commandContext.RegisterVariable(
                new VariableInfo("mat_picmip")
                .WithHelpInfo("If not 0, this is the number of times to halve the size of texture sizes")
                .WithValue(DefaultPicMip)
                .WithNumberFilter(true)
                .WithMinMaxFilter(ImageConversionUtils.MinSizeExponent, ImageConversionUtils.MaxSizeExponent, true));
        }

        /// <summary>
        /// Computes the scaled size of a texture that has the given width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public (int, int) ComputeScaledSize(int width, int height)
        {
            return ImageConversionUtils.ComputeScaledSize(width, height, _roundDown.Integer, _picMip.Integer);
        }

        private Image<Rgba32> InternalConvertTexture(IndexedColor256Texture inputTexture, TextureFormat textureFormat)
        {
            var pixels = ImageConversionUtils.ConvertIndexedToRgba32(inputTexture.Palette, inputTexture.Pixels, inputTexture.Width, inputTexture.Height, textureFormat);

            //Alpha tested textures have their fully transparent pixels modified so samplers won't sample the color used and blend it
            //This stops the color from bleeding through
            if (textureFormat == TextureFormat.AlphaTest
                || textureFormat == TextureFormat.IndexAlpha)
            {
                ImageConversionUtils.BoxFilter3x3(pixels, inputTexture.Width, inputTexture.Height);
            }

            //Rescale image to nearest power of 2
            (var scaledWidth, var scaledHeight) = ComputeScaledSize(inputTexture.Width, inputTexture.Height);

            var scaledPixels = ImageConversionUtils.ResampleTexture(new Span<Rgba32>(pixels), inputTexture.Width, inputTexture.Height, scaledWidth, scaledHeight);

            return Image.LoadPixelData(scaledPixels, scaledWidth, scaledHeight);
        }

        /// <summary>
        /// Converts an indexed 256 color texture to Rgba32
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <param name="textureFormat"></param>
        /// <returns></returns>
        public Image<Rgba32> ConvertTexture(IndexedColor256Texture inputTexture, TextureFormat textureFormat)
        {
            if (inputTexture == null)
            {
                throw new ArgumentNullException(nameof(inputTexture));
            }

            var pixels = ImageConversionUtils.ConvertIndexedToRgba32(inputTexture.Palette, inputTexture.Pixels, inputTexture.Width, inputTexture.Height, textureFormat);

            //Alpha tested textures have their fully transparent pixels modified so samplers won't sample the color used and blend it
            //This stops the color from bleeding through
            if (textureFormat == TextureFormat.AlphaTest
                || textureFormat == TextureFormat.IndexAlpha)
            {
                ImageConversionUtils.BoxFilter3x3(pixels, inputTexture.Width, inputTexture.Height);
            }

            //Rescale image to nearest power of 2
            (var scaledWidth, var scaledHeight) = ComputeScaledSize(inputTexture.Width, inputTexture.Height);

            var scaledPixels = ImageConversionUtils.ResampleTexture(new Span<Rgba32>(pixels), inputTexture.Width, inputTexture.Height, scaledWidth, scaledHeight);

            return Image.LoadPixelData(scaledPixels, scaledWidth, scaledHeight);
        }

        private ImageSharpTexture InternalLoadTexture(IndexedColor256Texture inputTexture, TextureFormat textureFormat)
        {
            var image = InternalConvertTexture(inputTexture, textureFormat);

            return new ImageSharpTexture(image, true);
        }

        /// <summary>
        /// Loads a texture and creates it using the specified factory
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <param name="textureFormat"></param>
        /// <param name="gd"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public Texture LoadTexture(IndexedColor256Texture inputTexture, TextureFormat textureFormat, GraphicsDevice gd, ResourceFactory factory)
        {
            if (inputTexture == null)
            {
                throw new ArgumentNullException(nameof(inputTexture));
            }

            var imageSharpTexture = InternalLoadTexture(inputTexture, textureFormat);

            return imageSharpTexture.CreateDeviceTexture(gd, factory);
        }

        /// <summary>
        /// Loads a texture and creates it using the specified graphics device's resource factory
        /// The texture is added to the given cache
        /// </summary>
        /// <param name="inputTexture"></param>
        /// <param name="textureFormat"></param>
        /// <param name="name"></param>
        /// <param name="gd"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public Texture LoadTexture(IndexedColor256Texture inputTexture, TextureFormat textureFormat, string name, GraphicsDevice gd, ResourceCache cache)
        {
            if (inputTexture == null)
            {
                throw new ArgumentNullException(nameof(inputTexture));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var imageSharpTexture = InternalLoadTexture(inputTexture, textureFormat);

            return cache.AddTexture2D(gd, gd.ResourceFactory, imageSharpTexture, name);
        }
    }
}
