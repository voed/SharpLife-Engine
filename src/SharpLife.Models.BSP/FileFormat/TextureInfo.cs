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
using System.Numerics;

namespace SharpLife.Models.BSP.FileFormat
{
    /// <summary>
    /// Each texture info contains data about a texture used on a surface
    /// S and T are normals indicating how to project the texture, with values to offset the texture in 2D space
    /// </summary>
    public class TextureInfo
    {
        public Vector3 SNormal { get; set; }

        public float SValue { get; set; }

        public Vector3 TNormal { get; set; }

        public float TValue { get; set; }

        public MipTexture MipTexture { get; set; }
        public TextureFlags Flags { get; set; }
    }
}
