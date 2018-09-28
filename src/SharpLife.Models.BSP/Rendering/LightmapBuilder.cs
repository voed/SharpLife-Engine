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

using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace SharpLife.Models.BSP.Rendering
{
    public sealed class LightmapBuilder
    {
        private readonly LightmapPool _pool;

        private readonly List<SingleTextureData> _textures = new List<SingleTextureData>();

        public int Width => _pool.Width;

        public int Height => _pool.Height;

        public LightmapBuilder(int width, int height)
        {
            _pool = new LightmapPool(width, height);
        }

        public Vector2? TryAllocate(Image<Rgba32> newImageData)
        {
            return _pool.TryAllocate(newImageData);
        }

        public void AddTextureData(SingleTextureData textureData)
        {
            _textures.Add(textureData);
        }

        public SingleLightmapData Build(ResourceLayout layout, ResourceCache resourceCache, GraphicsDevice gd, ResourceFactory factory)
        {
            var texture = _pool.Upload(resourceCache, gd, factory);

            _pool.Dispose();

            //TODO: cache resource sets for each texture
            return new SingleLightmapData
            {
                Lightmap = factory.CreateResourceSet(new ResourceSetDescription(
                    layout,
                    resourceCache.GetTextureView(factory, texture)
                )),
                Textures = _textures.ToArray()
            };
        }
    }
}
