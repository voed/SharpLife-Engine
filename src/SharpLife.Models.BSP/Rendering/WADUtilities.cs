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
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using System.Collections.Generic;
using Veldrid;

namespace SharpLife.Models.BSP.Rendering
{
    public static class WADUtilities
    {
        /// <summary>
        /// Uploads all of the given textures, adding them to the cache
        /// </summary>
        /// <param name="textures"></param>
        /// <param name="textureLoader"></param>
        /// <param name="gd"></param>
        /// <param name="cache"></param>
        public static void UploadTextures(IReadOnlyList<MipTexture> textures, TextureLoader textureLoader, GraphicsDevice gd, ResourceCache cache)
        {
            foreach (var texture in textures)
            {
                //Convert the image data to an ImageSharp image
                var textureFormat = texture.Name.StartsWith('{') ? TextureFormat.AlphaTest : TextureFormat.Normal;

                textureLoader.LoadTexture(
                    new IndexedColor256Image(
                        texture.Palette,
                        texture.Data[0],
                        (int)texture.Width,
                        (int)texture.Height),
                    textureFormat,
                    true,
                    texture.Name,
                    gd,
                    cache);
            }
        }
    }
}
