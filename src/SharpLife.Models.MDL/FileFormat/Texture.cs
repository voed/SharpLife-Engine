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

namespace SharpLife.Models.MDL.FileFormat
{
    public class Texture
    {
        public string Name { get; set; }
        public TextureFlags Flags { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public byte[] Pixels { get; set; }

        public Rgb24[] Palette { get; } = new Rgb24[IndexPaletteConstants.NumPaletteColors];
    }
}
