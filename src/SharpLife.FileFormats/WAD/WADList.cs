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
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.FileFormats.WAD
{
    /// <summary>
    /// Manages the list of wad files and handles texture lookup
    /// </summary>
    public class WADList
    {
        private readonly IFileSystem _fileSystem;

        private readonly string _extension;

        private readonly List<WADFile> _wadFiles = new List<WADFile>();

        /// <summary>
        /// Creates a new WAD list
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="extension">The file extension to use for WAD files</param>
        public WADList(IFileSystem fileSystem, string extension)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
        }

        public bool IsLoaded(string name)
        {
            return _wadFiles.Any(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public WADFile Get(string name)
        {
            return _wadFiles.Find(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void Load(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("WAD file name must be valid", nameof(name));
            }

            name = FileExtensionUtils.EnsureExtension(name, _extension);

            if (IsLoaded(name))
            {
                throw new ArgumentException($"WAD file '{name}' is already loaded");
            }

            var file = new WADLoader(_fileSystem.OpenRead(name)).ReadWADFile();

            file.Name = name;

            _wadFiles.Add(file);
        }

        public void Add(WADFile wadFile)
        {
            if (wadFile == null)
            {
                throw new ArgumentNullException(nameof(wadFile));
            }

            wadFile.Name = FileExtensionUtils.EnsureExtension(wadFile.Name, _extension);

            if (IsLoaded(wadFile.Name))
            {
                throw new ArgumentException($"WAD file '{wadFile.Name}' is already loaded");
            }

            _wadFiles.Add(wadFile);
        }

        public void Clear()
        {
            _wadFiles.Clear();
        }

        /// <summary>
        /// Finds a texture by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MipTexture FindTexture(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            foreach (var wadFile in _wadFiles)
            {
                foreach (var texture in wadFile.MipTextures)
                {
                    if (texture.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the WAD file that contains the given texture, or null if isn't contained in any WAD file
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public WADFile GetWADFromTexture(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            foreach (var wadFile in _wadFiles)
            {
                foreach (var texture in wadFile.MipTextures)
                {
                    if (texture.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return wadFile;
                    }
                }
            }

            return null;
        }
    }
}
