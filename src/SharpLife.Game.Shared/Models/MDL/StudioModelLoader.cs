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

using SharpLife.FileSystem;
using SharpLife.Models;
using SharpLife.Models.MDL.FileFormat;
using System;
using System.IO;

namespace SharpLife.Game.Shared.Models.MDL
{
    public sealed class StudioModelLoader : IModelLoader
    {
        public IModel Load(string name, IFileSystem fileSystem, BinaryReader reader, Delegates.AddModel addModelCallback, bool computeCRC)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            //Check if we can actually load this
            if (!StudioLoader.IsStudioFile(reader))
            {
                return null;
            }

            var loader = new StudioLoader(reader);

            (var studioFile, var rawSequences) = loader.ReadStudioFile();

            var baseName = Path.GetFileNameWithoutExtension(name);
            var extension = Path.GetExtension(name);

            if (studioFile.Textures == null)
            {
                //Read the textures
                var textureFileName = Path.Combine(Path.GetDirectoryName(name), baseName + "T" + extension);

                var textureLoader = new StudioLoader(new BinaryReader(fileSystem.OpenRead(textureFileName)));

                (var textureFile, _) = textureLoader.ReadStudioFile();

                //Merge into main file
                studioFile.Textures = textureFile.Textures;
                studioFile.Skins = textureFile.Skins;
            }

            //Read animation data from sequence files
            for (var i = 1; i < studioFile.SequenceGroups.Count; ++i)
            {
                var sequenceFileName = Path.Combine(Path.GetDirectoryName(name), baseName + i.ToString("D2") + extension);

                var sequenceLoader = new StudioSequenceLoader(new BinaryReader(fileSystem.OpenRead(sequenceFileName)));

                sequenceLoader.ReadAnimations(studioFile, i, rawSequences);
            }

            uint crc = 0;

            if (computeCRC)
            {
                crc = loader.ComputeCRC();
            }

            return new StudioModel(name, crc, studioFile);
        }
    }
}
