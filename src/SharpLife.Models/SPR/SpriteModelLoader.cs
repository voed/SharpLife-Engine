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

using SharpLife.FileFormats.SPR;
using System;
using System.IO;

namespace SharpLife.Models.SPR
{
    public sealed class SpriteModelLoader : IModelLoader
    {
        public IModel Load(string name, BinaryReader reader, bool computeCRC)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            //Check if we can actually load this
            if (!SpriteLoader.IsSpriteFile(reader))
            {
                return null;
            }

            var loader = new SpriteLoader(reader);

            var spriteFile = loader.ReadSpriteFile();

            uint crc = 0;

            if (computeCRC)
            {
                crc = loader.ComputeCRC();
            }

            return new SpriteModel(name, crc, spriteFile);
        }
    }
}
