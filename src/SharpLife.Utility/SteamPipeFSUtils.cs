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
using System;

namespace SharpLife.Utility
{
    public static class SteamPipeFSUtils
    {
        /// <summary>
        /// Sets up the filesystem with the given parameters
        /// </summary>
        /// <param name="fileSystem">Filesystem to set up</param>
        /// <param name="baseDirectory">Base directory for all search paths</param>
        /// <param name="defaultGameDirectory">Default game directory to search for content when not found in the game directory</param>
        /// <param name="gameDirectory">The game directory to search for content</param>
        /// <param name="defaultLanguage">Default game language</param>
        /// <param name="language">Current game language</param>
        /// <param name="lowViolence">Whether to add low violence search paths</param>
        /// <param name="enableHDModels">Whether to add HD models search paths</param>
        /// <param name="enableAddonsFolder">Whether to add the addons search paths</param>
        public static void SetupFileSystem(this IFileSystem fileSystem,
            string baseDirectory,
            string defaultGameDirectory,
            string gameDirectory,
            string defaultLanguage,
            string language,
            bool lowViolence,
            bool enableHDModels,
            bool enableAddonsFolder)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException(nameof(baseDirectory));
            }

            if (string.IsNullOrWhiteSpace(defaultGameDirectory))
            {
                throw new ArgumentException(nameof(defaultGameDirectory));
            }

            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                throw new ArgumentException(nameof(gameDirectory));
            }

            if (string.IsNullOrWhiteSpace(defaultLanguage))
            {
                throw new ArgumentException(nameof(defaultLanguage));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException(nameof(language));
            }

            var addLanguage = language != defaultLanguage;

            void AddGameDirectories(string gameDirectoryName, string pathID)
            {
                if (lowViolence)
                {
                    fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectoryName}{FileSystemConstants.Suffixes.LowViolence}", pathID, false);
                }

                if (enableAddonsFolder)
                {
                    fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectoryName}{FileSystemConstants.Suffixes.Addon}", pathID, false);
                }

                if (addLanguage)
                {
                    fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectoryName}_{language}", pathID, false);
                }

                if (enableHDModels)
                {
                    fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectoryName}{FileSystemConstants.Suffixes.HD}", pathID, false);
                }
            }

            AddGameDirectories(gameDirectory, FileSystemConstants.PathID.Game);

            fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectory}", FileSystemConstants.PathID.Game);
            fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectory}", FileSystemConstants.PathID.GameConfig);

            fileSystem.AddSearchPath($"{baseDirectory}/{gameDirectory}{FileSystemConstants.Suffixes.Downloads}", FileSystemConstants.PathID.GameDownload);

            AddGameDirectories(defaultGameDirectory, FileSystemConstants.PathID.DefaultGame);

            fileSystem.AddSearchPath(baseDirectory, FileSystemConstants.PathID.Base);

            fileSystem.AddSearchPath($"{baseDirectory}/{defaultGameDirectory}", FileSystemConstants.PathID.Game, false);

            fileSystem.AddSearchPath($"{baseDirectory}/{FileSystemConstants.PlatformDirectory}", FileSystemConstants.PathID.Platform);
        }
    }
}
