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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpLife.FileSystem
{
    /// <summary>
    /// Provides a filesystem that accesses on-disk files
    /// </summary>
    public sealed class DiskFileSystem : IFileSystem
    {
        private class SearchPath
        {
            /// <summary>
            /// The Id of this path
            /// File operations can filter on the id to load from specific paths
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// The base path to prepend to file paths when searching for files
            /// These are absolute
            /// </summary>
            public string BasePath { get; }

            /// <summary>
            /// Whether this path allows write operations
            /// </summary>
            public bool WriteAccess { get; }

            public SearchPath(string id, string basePath, bool writeAccess)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                BasePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
                WriteAccess = writeAccess;
            }
        }

        private List<SearchPath> SearchPaths { get; } = new List<SearchPath>();

        public void AddSearchPath(string basePath, string pathID, bool writeAccess = true)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (string.IsNullOrWhiteSpace(pathID))
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            basePath = Path.GetFullPath(basePath);

            if (SearchPaths.Any(s => s.BasePath == basePath && s.Id == pathID))
            {
                throw new InvalidOperationException($"The path \"{basePath}\" can only be added once for path ID \"{pathID}\"");
            }

            //Intern path IDs since they're used as literals anyway, this ensures the memory doesn't get duplicated
            pathID = string.Intern(pathID);

            SearchPaths.Add(new SearchPath(pathID, basePath, writeAccess));
        }

        public bool RemoveSearchPath(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            basePath = Path.GetFullPath(basePath);

            return SearchPaths.RemoveAll(s => s.BasePath == basePath) > 0;
        }

        public void RemoveAllSearchPaths()
        {
            SearchPaths.Clear();
            SearchPaths.TrimExcess();
        }

        private string InternalGetAbsolutePath(string relativePath, string pathID = null)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            foreach (var searchPath in SearchPaths)
            {
                if (pathID != null && searchPath.Id != pathID)
                {
                    continue;
                }

                var absolutePath = Path.Combine(searchPath.BasePath, relativePath);

                if (Directory.Exists(absolutePath) || File.Exists(absolutePath))
                {
                    return absolutePath;
                }
            }

            return null;
        }

        public string GetAbsolutePath(string relativePath, string pathID = null)
        {
            var result = InternalGetAbsolutePath(relativePath, pathID);

            if (result != null)
            {
                return result;
            }

            throw new FileNotFoundException("Could not resolve absolute path", relativePath);
        }

        public string GetAbsolutePathOrDefault(string relativePath, string pathID = null, string defaultValue = null)
        {
            var result = InternalGetAbsolutePath(relativePath, pathID);

            return result ?? defaultValue;
        }

        public string GetRelativePath(string absolutePath, string pathID = null)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentNullException(nameof(absolutePath));
            }

            foreach (var searchPath in SearchPaths)
            {
                if (pathID != null && searchPath.Id != pathID)
                {
                    continue;
                }

                if (absolutePath.StartsWith(searchPath.BasePath))
                {
                    return absolutePath.Substring(searchPath.BasePath.Length);
                }
            }

            throw new FileNotFoundException("Could not resolve relative path", absolutePath);

            //TODO: add OrDefault overload, remove from extensions
        }

        public string GetWritePath(string relativePath, string pathID = null)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            foreach (var searchPath in SearchPaths)
            {
                if (pathID != null && searchPath.Id != pathID)
                {
                    continue;
                }

                var absolutePath = Path.Combine(searchPath.BasePath, relativePath);

                //Create the directory so it exists when code tries to write to it
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

                return absolutePath;
            }

            throw new FileNotFoundException("Could not construct write path", relativePath);
        }

        public bool CreateDirectoryHierarchy(string relativePath, string pathID = null)
        {
            var absolutePath = GetAbsolutePath(relativePath, pathID);

            try
            {
                Directory.CreateDirectory(absolutePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Exists(string fileName, string pathID = null)
        {
            var path = GetAbsolutePathOrDefault(fileName, pathID);

            return path != null ? File.Exists(path) : false;
        }

        public Stream Open(string relativePath, FileMode mode, FileAccess access, FileShare share, string pathID = null)
        {
            var isRead = mode == FileMode.Append
                    || mode == FileMode.Open
                    || mode == FileMode.Truncate;

            var absolutePath = isRead ? GetAbsolutePath(relativePath, pathID) : GetWritePath(relativePath, pathID);

            return File.Open(
                absolutePath,
                mode,
                access,
                share);
        }
    }
}
