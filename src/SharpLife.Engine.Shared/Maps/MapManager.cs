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

using SharpLife.FileFormats.BSP;
using SharpLife.FileSystem;
using SharpLife.Utility;
using System;
using System.IO;

namespace SharpLife.Engine.Shared.Maps
{
    public class MapManager : IMapManager
    {
        private readonly IFileSystem _fileSystem;

        private readonly string _mapDirectory;

        private readonly string _bspExtension;

        public string MapName { get; private set; }

        public string PreviousMapName { get; private set; }

        public BSPFile BSPFile { get; private set; }

        public event Action OnClear;

        public MapManager(IFileSystem fileSystem, string mapDirectory, string bspExtension)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _mapDirectory = mapDirectory ?? throw new ArgumentNullException(nameof(mapDirectory));
            _bspExtension = bspExtension ?? throw new ArgumentNullException(nameof(bspExtension));
        }

        public string FormatMapFileName(string mapName)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            return Path.Combine(_mapDirectory, mapName + FileExtensionUtils.AsExtension(_bspExtension));
        }

        public bool IsMapValid(string mapName)
        {
            return _fileSystem.Exists(FormatMapFileName(mapName));
        }

        public bool LoadMap(string mapName)
        {
            var fileName = FormatMapFileName(mapName);

            try
            {
                BSPFile = FileFormats.BSP.Input.ReadBSPFile(_fileSystem.OpenRead(fileName));

                BSPFile.Name = fileName;
            }
            catch (InvalidBSPVersionException e)
            {
                Console.WriteLine($"Error loading map: {e.Message}");
                return false;
            }

            //TODO: this may be better off stored elsewhere
            PreviousMapName = MapName;

            MapName = mapName;

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
    }
}
