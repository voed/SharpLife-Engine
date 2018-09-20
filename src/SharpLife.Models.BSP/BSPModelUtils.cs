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

using SharpLife.Utility.FileSystem;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SharpLife.Models.BSP
{
    public sealed class BSPModelUtils
    {
        private readonly string _bspModelNamePrefix;

        private readonly string _mapsDirectory;

        private readonly string _bspExtension;

        private readonly string _mapFileNameBaseRegexString;

        public BSPModelUtils(string bspModelNamePrefix, string mapsDirectory, string bspExtension)
        {
            _bspModelNamePrefix = bspModelNamePrefix ?? throw new ArgumentNullException(nameof(bspModelNamePrefix));
            _mapsDirectory = mapsDirectory ?? throw new ArgumentNullException(nameof(mapsDirectory));
            _bspExtension = bspExtension ?? throw new ArgumentNullException(nameof(bspExtension));

            _mapFileNameBaseRegexString =
            mapsDirectory
            + $"[{Regex.Escape(Path.DirectorySeparatorChar.ToString() + Path.AltDirectorySeparatorChar.ToString())}](\\w+)"
            + Regex.Escape(FileExtensionUtils.AsExtension(bspExtension));
        }

        public bool IsBSPModelName(string modelName)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            return modelName.StartsWith(_bspModelNamePrefix);
        }

        /// <summary>
        /// Formats a map name as a file name
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public string FormatMapFileName(string mapName)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            return Path.Combine(_mapsDirectory, mapName + FileExtensionUtils.AsExtension(_bspExtension));
        }

        /// <summary>
        /// Extracts the base map name from a file name
        /// </summary>
        /// <param name="mapFileName"></param>
        /// <returns></returns>
        /// <exception cref="FormatException">If the file name is not a map file name</exception>
        public string ExtractMapBaseName(string mapFileName)
        {
            if (mapFileName == null)
            {
                throw new ArgumentNullException(nameof(mapFileName));
            }

            var match = Regex.Match(mapFileName, _mapFileNameBaseRegexString);

            if (!match.Success)
            {
                throw new FormatException($"Could not extract map base name from {mapFileName}");
            }

            return match.Groups[1].Captures[0].Value;
        }
    }
}
