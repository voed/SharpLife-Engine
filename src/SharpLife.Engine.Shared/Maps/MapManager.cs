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

using Serilog;
using SharpLife.FileFormats.BSP;
using SharpLife.FileSystem;
using SharpLife.Utility;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SharpLife.Engine.Shared.Maps
{
    public class MapManager : IMapManager
    {
        private readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;

        private readonly string _mapDirectory;

        private readonly string _bspExtension;

        private readonly string _mapFileNameBaseRegexString;

        public string MapName { get; private set; }

        public string PreviousMapName { get; private set; }

        public BSPFile BSPFile { get; private set; }

        public event Action OnClear;

        public MapManager(ILogger logger, IFileSystem fileSystem, string mapDirectory, string bspExtension)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _mapDirectory = mapDirectory ?? throw new ArgumentNullException(nameof(mapDirectory));
            _bspExtension = bspExtension ?? throw new ArgumentNullException(nameof(bspExtension));

            //Match on a string like "maps/bounce.bsp"
            _mapFileNameBaseRegexString =
                _mapDirectory
                + $"[{Regex.Escape(Path.DirectorySeparatorChar.ToString() + Path.AltDirectorySeparatorChar.ToString())}](\\w+)"
                + Regex.Escape(FileExtensionUtils.AsExtension(_bspExtension));
        }

        public string FormatMapFileName(string mapName)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            return Path.Combine(_mapDirectory, mapName + FileExtensionUtils.AsExtension(_bspExtension));
        }

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

        public bool IsMapValid(string mapName)
        {
            return _fileSystem.Exists(FormatMapFileName(mapName));
        }

        public bool LoadMap(string mapFileName)
        {
            try
            {
                BSPFile = FileFormats.BSP.Input.ReadBSPFile(_fileSystem.OpenRead(mapFileName));

                BSPFile.Name = mapFileName;
            }
            catch (InvalidBSPVersionException e)
            {
                _logger.Error($"Error loading map: {e.Message}");
                return false;
            }

            //TODO: this may be better off stored elsewhere
            PreviousMapName = MapName;

            MapName = ExtractMapBaseName(mapFileName);

            return true;
        }

        public void Clear()
        {
            if (BSPFile != null)
            {
                OnClear?.Invoke();

                BSPFile = null;
                MapName = null;
            }
        }

        public bool ComputeCRC(string mapName, out uint crc)
        {
            var fileName = FormatMapFileName(mapName);

            try
            {
                crc = FileFormats.BSP.Input.ComputeCRC(_fileSystem.OpenRead(fileName));
            }
            catch (Exception e)
            {
                if (e is InvalidOperationException
                    || e is InvalidBSPVersionException
                    || e is IOException)
                {
                    crc = 0;
                    return false;
                }

                throw;
            }

            return true;
        }
    }
}
