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

using System.IO;

namespace SharpLife.FileSystem
{
    /// <summary>
    /// Represents the engine's filesystem
    /// Supports SteamPipe directories
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Adds a search path
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="pathID"></param>
        /// <param name="writeAccess"></param>
        void AddSearchPath(string basePath, string pathID, bool writeAccess = true);

        /// <summary>
        /// Removes all search paths with the given base path
        /// </summary>
        /// <param name="basePath"></param>
        /// <returns></returns>
        bool RemoveSearchPath(string basePath);

        /// <summary>
        /// Removes all search paths
        /// </summary>
        void RemoveAllSearchPaths();

        /// <summary>
        /// Given a relative path, returns the absolute path to it if it exists
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="pathID">If not null, specifies that only paths with this Id should be checked for the file's existence</param>
        /// <param name="mustExist">If false, only the path to the file must exist, otherwise the file must also exist</param>
        /// <returns>The absolute path if it could be resolved</returns>
        /// <exception cref="FileNotFoundException">If the file could not be resolved</exception>
        string GetAbsolutePath(string relativePath, string pathID = null, bool mustExist = true);

        /// <summary>
        /// Given an absolute path, attempts to resolve the path to a relative path
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <param name="pathID">If not null, specifies that only paths with this Id should be used to resolve the path</param>
        /// <returns>The relative path if it could be resolved</returns>
        /// <exception cref="FileNotFoundException">If the file could not be resolved</exception>
        string GetRelativePath(string absolutePath, string pathID = null);

        /// <summary>
        /// Given a relative path, attempts to construct an absolute path that can be written to
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="pathID">If not null, specifies that only paths with this Id should be used to construct the path</param>
        /// <returns>The absolute path if it could be constructed</returns>
        /// <exception cref="FileNotFoundException">If the file could not be resolved</exception>
        string GetWritePath(string relativePath, string pathID = null);

        /// <summary>
        /// Creates all directories for the given path
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        bool CreateDirectoryHierarchy(string relativePath, string pathID = null);

        /// <summary>
        /// Checks whether the file exists
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="pathID">If not null, the ID of the paths to check for the file's existence, otherwise all paths are searched</param>
        /// <returns></returns>
        bool Exists(string fileName, string pathID = null);

        /// <summary>
        /// Opens a file as a stream
        /// <see cref="File.Open(string, FileMode, FileAccess, FileShare)"/>
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="share"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        Stream Open(string relativePath, FileMode mode, FileAccess access, FileShare share, string pathID = null);
    }
}
