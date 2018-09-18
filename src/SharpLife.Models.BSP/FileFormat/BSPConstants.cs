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

namespace SharpLife.Models.BSP.FileFormat
{
    public static class BSPConstants
    {
        //TODO: hulls max is a common constant
        public const int MaxHulls = 4;

        public const int MaxModels = 400;
        public const int MaxBrushes = 4096;
        public const int MaxEntities = 1024;
        public const int MaxEntString = (128 * 1024);

        public const int MaxPlanes = 32767;
        public const int MaxNodes = 32767;		// because negative shorts are contents
        public const int MaxClipNodes = 32767;
        public const int MaxLeafs = 8192;
        public const int MaxVerts = 65535;
        public const int MaxFaces = 65535;
        public const int MaxMarkSurfaces = 65535;
        public const int MaxTexInfo = 8192;
        public const int MaxEdges = 256000;
        public const int MaxSurfEdges = 512000;
        public const int MaxTextures = 512;
        public const int MaxMiptex = 0x200000;
        public const int MaxLighting = 0x200000;
        public const int MaxVisibility = 0x200000;

        public const int MaxPortals = 65536;

        public const int MaxLightmaps = 4;

        public const int MaxLightStyles = 64;

        /// <summary>
        /// Indicates that a face style is not used
        /// </summary>
        public const int NoLightStyle = 255;

        /// <summary>
        /// Scale of a lightmap relative to a texture pixel
        /// If texture scale is 1 1, then a texture pixel takes up a 1x1 physical pixel,
        /// whereas a lightmap pixel takes up a 16x16 physical pixel
        /// </summary>
        public const int LightmapScale = 16;
    }
}
