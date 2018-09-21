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
using System.Numerics;

namespace SharpLife.Models.BSP.Rendering
{
    public struct BSPSurfaceData
    {
        public WorldTextureCoordinate WorldTexture;
        public Vector2 Lightmap;

        public float LightmapXOffset;

        public int Style0;
        public int Style1;
        public int Style2;
        public int Style3;
    }
}
