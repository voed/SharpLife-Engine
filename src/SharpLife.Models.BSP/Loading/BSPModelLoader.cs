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
using SharpLife.Models.BSP.FileFormat;
using System;
using System.IO;

namespace SharpLife.Models.BSP.Loading
{
    public sealed class BSPModelLoader : IModelLoader
    {
        private readonly string _bspModelNamePrefix;

        public BSPModelLoader(string bspModelNamePrefix)
        {
            _bspModelNamePrefix = bspModelNamePrefix ?? throw new ArgumentNullException(nameof(bspModelNamePrefix));
        }

        public IModel Load(string name, IFileSystem fileSystem, BinaryReader reader, Delegates.AddModel addModelCallback, bool computeCRC)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            //Check if we can actually load this
            //TODO: because BSP files don't have a separate identifier, this will fail on invalid BSP versions
            //Should remove this once the other formats can be loaded
            if (!BSPLoader.IsBSPFile(reader))
            {
                return null;
            }

            var loader = new BSPLoader(reader);

            var bspFile = loader.ReadBSPFile();

            uint crc = 0;

            if (computeCRC)
            {
                crc = loader.ComputeCRC();
            }

            //add all of its submodels
            //First submodel (0) is the world
            for (var i = 1; i < bspFile.Models.Count; ++i)
            {
                var subModelName = $"{_bspModelNamePrefix}{i}";
                addModelCallback(subModelName, new BSPModel(subModelName, crc, bspFile, bspFile.Models[i]));
            }

            return new BSPModel(name, crc, bspFile, bspFile.Models[0]);
        }
    }
}
