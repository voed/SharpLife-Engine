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

using System;
using System.IO;

namespace SharpLife.Utility
{
    public static class FileExtensionUtils
    {
        /// <summary>
        /// Returns an extension constant as a string that can be appended to paths
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string AsExtension(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (extension.StartsWith('.'))
            {
                return extension;
            }

            return $".{extension}";
        }

        /// <summary>
        /// Ensures that the given path has the given extension
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string EnsureExtension(string path, string extension)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            return Path.ChangeExtension(path, extension);
        }
    }
}
