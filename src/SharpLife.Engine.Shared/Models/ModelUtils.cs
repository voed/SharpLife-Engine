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

using SharpLife.Engine.API.Engine.Shared;
using SharpLife.Utility;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SharpLife.Engine.Shared.Models
{
    public static class ModelUtils
    {
        private static readonly string _mapFileNameBaseRegexString =
            Framework.Directory.Maps
            + $"[{Regex.Escape(Path.DirectorySeparatorChar.ToString() + Path.AltDirectorySeparatorChar.ToString())}](\\w+)"
            + Regex.Escape(FileExtensionUtils.AsExtension(Framework.Extension.BSP));

        public static ModelIndex CreateModelIndex(int index)
        {
            return new ModelIndex(index + 1);
        }

        public static int GetInternalIndex(ModelIndex index)
        {
            return index.Index - 1;
        }

        public static bool IsBSPModelName(string modelName)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            return modelName.StartsWith(Framework.BSPModelNamePrefix);
        }

        /// <summary>
        /// Formats a map name as a file name
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public static string FormatMapFileName(string mapName)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            return Path.Combine(Framework.Directory.Maps, mapName + FileExtensionUtils.AsExtension(Framework.Extension.BSP));
        }

        /// <summary>
        /// Extracts the base map name from a file name
        /// </summary>
        /// <param name="mapFileName"></param>
        /// <returns></returns>
        /// <exception cref="FormatException">If the file name is not a map file name</exception>
        public static string ExtractMapBaseName(string mapFileName)
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
