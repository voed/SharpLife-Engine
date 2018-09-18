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
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;

namespace SharpLife.Models.BSP.FileFormat
{
    public class BSPFile
    {
        public BSPVersion Version { get; set; }

        public List<MipTexture> MipTextures { get; set; }

        public List<Plane> Planes { get; set; }

        public List<Face> Faces { get; set; }

        public List<Leaf> Leaves { get; set; }

        public List<Model> Models { get; set; }

        public List<Node> Nodes { get; set; }

        public List<ClipNode> ClipNodes { get; set; }

        public string Entities { get; set; }

        public byte[] Visibility { get; set; }

        public Rgb24[] Lighting { get; set; }

        /// <summary>
        /// Whether the BSP file uses Blue Shift style lump layout
        /// In this style the entities and plane lumps are swapped
        /// </summary>
        public bool HasBlueShiftLumpLayout { get; set; }
    }
}
