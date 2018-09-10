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

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;

namespace SharpLife.Renderer.Utility
{
    /// <summary>
    /// Provides a way to allocate lightmaps from a pool of memory that can be uploaded into memory
    /// </summary>
    public class LightmapPool : IDisposable
    {
        private readonly Image<Rgba32> _image;

        //Keeps track of how many rows are allocated in a column
        private readonly int[] _allocated;

        public int Width => _image.Width;

        public int Height => _image.Height;

        public LightmapPool(int width, int height)
        {
            _image = new Image<Rgba32>(width, height);

            _allocated = new int[width];
        }

        /// <summary>
        /// Tries to allocate space for the given image
        /// If there is not enough space, returns null
        /// </summary>
        /// <param name="newImageData"></param>
        /// <returns></returns>
        public Vector2? TryAllocate(Image<Rgba32> newImageData)
        {
            if (newImageData == null)
            {
                throw new ArgumentException(nameof(newImageData));
            }

            if (newImageData.Width > _image.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(newImageData), "Requested image width is too large");
            }

            if (newImageData.Height > _image.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(newImageData), "Requested image height is too large");
            }

            var coordinates = new SixLabors.Primitives.Point();

            int mostAllocatedWholeImage = _image.Height;

            for (var startWidth = 0; startWidth < _allocated.Length - newImageData.Width; ++startWidth)
            {
                //Tracks the most allocations in a line; this is the starting point for the data if this section is valid
                int mostAllocated = 0;

                int currentWidth;

                for (currentWidth = 0; currentWidth < newImageData.Width; ++currentWidth)
                {
                    //Line is full
                    if (_allocated[startWidth + currentWidth] >= _image.Height)
                    {
                        break;
                    }

                    if (_allocated[startWidth + currentWidth] >= mostAllocated)
                    {
                        mostAllocated = _allocated[startWidth + currentWidth];
                    }
                }

                if (currentWidth == newImageData.Width)
                {
                    coordinates.X = startWidth;
                    coordinates.Y = mostAllocatedWholeImage = mostAllocated;
                }
            }

            if (mostAllocatedWholeImage + newImageData.Height <= _image.Height)
            {
                //There's room

                //Update allocated to cover image and padding between previous data on each line
                for (var x = 0; x < newImageData.Width; ++x)
                {
                    _allocated[coordinates.X + x] = mostAllocatedWholeImage + newImageData.Height;
                }

                var graphicsOptions = GraphicsOptions.Default;

                graphicsOptions.BlenderMode = PixelBlenderMode.Src;

                //Add the image data to the current lightmap image
                _image.Mutate(context => context.DrawImage(graphicsOptions, newImageData, coordinates));

                return new Vector2(coordinates.X, coordinates.Y);
            }

            return null;
        }

        public Texture Upload(ResourceCache resourceCache, GraphicsDevice gd, ResourceFactory factory)
        {
            if (resourceCache == null)
            {
                throw new ArgumentNullException(nameof(resourceCache));
            }

            var texture = resourceCache.AddTexture2D(
                    gd,
                    factory,
                    new ImageSharpTexture(_image, false), $"lightmap{resourceCache.GenerateUniqueId()}");

            //Reset the image to black
            _image.Mutate(context => context.Fill(Rgba32.Black));

            Array.Fill(_allocated, 0);

            return texture;
        }

        public void Dispose()
        {
            _image.Dispose();
        }
    }
}
