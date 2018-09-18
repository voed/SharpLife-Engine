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


using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.Models.BSP.FileFormat
{
    public class Face
    {
        public Plane Plane { get; set; }
        public bool Side { get; set; }

        public List<Vector3> Points { get; set; }

        public TextureInfo TextureInfo { get; set; }

        // lighting info
        public byte[] Styles { get; set; }
        //TODO
        public int LightOffset { get; set; }		// start of [numstyles*surfsize] samples

        public int[] Extents { get; } = new int[2];

        public int[] TextureMins { get; } = new int[2];
    }
}
