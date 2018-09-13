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

namespace SharpLife.FileFormats.WAD
{
    public static class WADConstants
    {
        /// <summary>
        /// Maximum byte length for texture names, excluding null terminator
        /// </summary>
        public const int MaxTextureNameLength = 15;
        public const int NumMipLevels = 4;

        //TODO: remove and use the constants in IndexPaletteConstants
        public const int NumPaletteColors = 256;
        public const int NumPaletteComponents = 3;
        public const int PaletteComponentSizeInBytes = 1;

        public const int PaletteSizeInBytes = NumPaletteColors * NumPaletteComponents * PaletteComponentSizeInBytes;
    }
}
