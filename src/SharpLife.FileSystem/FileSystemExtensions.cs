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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpLife.FileSystem
{
    /// <summary>
    /// Extensions for the filesystem that add non-essential methods
    /// </summary>
    public static class FileSystemExtensions
    {
        /// <summary>
        /// Gets the relative path, or a default value if it could not be resolved
        /// <see cref="IFileSystem.GetRelativePath(string, string)(string, string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="absolutePath"></param>
        /// <param name="pathID"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetRelativePathOrDefault(this IFileSystem self, string absolutePath, string pathID = null, string defaultValue = null)
        {
            try
            {
                return self.GetRelativePath(absolutePath, pathID);
            }
            catch (FileNotFoundException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets the write path, or a default value if it could not be constructed
        /// <see cref="IFileSystem.GetWritePath(string, string)(string, string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetWritePathOrDefault(this IFileSystem self, string relativePath, string pathID = null, string defaultValue = null)
        {
            try
            {
                return self.GetWritePath(relativePath, pathID);
            }
            catch (FileNotFoundException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// <see cref="File.ReadAllText(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static string ReadAllText(this IFileSystem self, string relativePath, string pathID = null)
        {
            return File.ReadAllText(self.GetAbsolutePath(relativePath, pathID));
        }

        /// <summary>
        /// <see cref="File.ReadAllText(string, Encoding)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="encoding"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static string ReadAllText(this IFileSystem self, string relativePath, Encoding encoding, string pathID = null)
        {
            return File.ReadAllText(self.GetAbsolutePath(relativePath, pathID), encoding);
        }

        /// <summary>
        /// <see cref="File.ReadAllLines(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static string[] ReadAllLines(this IFileSystem self, string relativePath, string pathID = null)
        {
            return File.ReadAllLines(self.GetAbsolutePath(relativePath, pathID));
        }

        /// <summary>
        /// <see cref="File.ReadAllLines(string, Encoding)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="encoding"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static string[] ReadAllLines(this IFileSystem self, string relativePath, Encoding encoding, string pathID = null)
        {
            return File.ReadAllLines(self.GetAbsolutePath(relativePath, pathID), encoding);
        }

        /// <summary>
        /// <see cref="File.ReadAllBytes(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static byte[] ReadAllBytes(this IFileSystem self, string relativePath, string pathID = null)
        {
            return File.ReadAllBytes(self.GetAbsolutePath(relativePath, pathID));
        }

        /// <summary>
        /// <see cref="File.WriteAllText(string, string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="contents"></param>
        /// <param name="pathID"></param>
        public static void WriteAllText(this IFileSystem self, string relativePath, string contents, string pathID = null)
        {
            File.WriteAllText(self.GetAbsolutePath(relativePath, pathID), contents);
        }

        /// <summary>
        /// <see cref="File.WriteAllText(string, string, Encoding)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="contents"></param>
        /// <param name="encoding"></param>
        /// <param name="pathID"></param>
        public static void WriteAllText(this IFileSystem self, string relativePath, string contents, Encoding encoding, string pathID = null)
        {
            File.WriteAllText(self.GetAbsolutePath(relativePath, pathID), contents, encoding);
        }

        /// <summary>
        /// <see cref="File.WriteAllLines(string, string[])"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="contents"></param>
        /// <param name="pathID"></param>
        public static void WriteAllLines(this IFileSystem self, string relativePath, string[] contents, string pathID = null)
        {
            File.WriteAllLines(self.GetAbsolutePath(relativePath, pathID), contents);
        }

        /// <summary>
        /// <see cref="File.WriteAllLines(string, IEnumerable{string})"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="contents"></param>
        /// <param name="pathID"></param>
        public static void WriteAllLines(this IFileSystem self, string relativePath, IEnumerable<string> contents, string pathID = null)
        {
            File.WriteAllLines(self.GetAbsolutePath(relativePath, pathID), contents);
        }

        /// <summary>
        /// <see cref="File.WriteAllLines(string, string[], Encoding)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="contents"></param>
        /// <param name="encoding"></param>
        /// <param name="pathID"></param>
        public static void WriteAllLines(this IFileSystem self, string relativePath, string[] contents, Encoding encoding, string pathID = null)
        {
            File.WriteAllLines(self.GetAbsolutePath(relativePath, pathID), contents, encoding);
        }

        /// <summary>
        /// <see cref="File.WriteAllLines(string, IEnumerable{string}, Encoding)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="contents"></param>
        /// <param name="encoding"></param>
        /// <param name="pathID"></param>
        public static void WriteAllLines(this IFileSystem self, string relativePath, IEnumerable<string> contents, Encoding encoding, string pathID = null)
        {
            File.WriteAllLines(self.GetAbsolutePath(relativePath, pathID), contents, encoding);
        }

        /// <summary>
        /// <see cref="File.WriteAllBytes(string, byte[])"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="bytes"></param>
        /// <param name="pathID"></param>
        public static void WriteAllBytes(this IFileSystem self, string relativePath, byte[] bytes, string pathID = null)
        {
            File.WriteAllBytes(self.GetAbsolutePath(relativePath, pathID), bytes);
        }

        /// <summary>
        /// <see cref="File.Open(string, FileMode)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="mode"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static Stream Open(this IFileSystem self, string relativePath, FileMode mode, string pathID = null)
        {
            return self.Open(relativePath, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None, pathID);
        }

        /// <summary>
        /// <see cref="File.Open(string, FileMode, FileAccess)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static Stream Open(this IFileSystem self, string relativePath, FileMode mode, FileAccess access, string pathID = null)
        {
            return self.Open(relativePath, mode, access, FileShare.None, pathID);
        }

        /// <summary>
        /// <see cref="File.OpenRead(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static Stream OpenRead(this IFileSystem self, string relativePath, string pathID = null)
        {
            return self.Open(relativePath, FileMode.Open, FileAccess.Read, FileShare.Read, pathID);
        }

        /// <summary>
        /// <see cref="File.OpenWrite(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static Stream OpenWrite(this IFileSystem self, string relativePath, string pathID = null)
        {
            return self.Open(relativePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, pathID);
        }

        /// <summary>
        /// <see cref="File.OpenText(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static StreamReader OpenText(this IFileSystem self, string relativePath, string pathID = null)
        {
            return new StreamReader(self.OpenRead(relativePath, pathID));
        }

        /// <summary>
        /// <see cref="File.CreateText(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static StreamWriter CreateText(this IFileSystem self, string relativePath, string pathID = null)
        {
            return new StreamWriter(self.OpenWrite(relativePath, pathID));
        }

        /// <summary>
        /// <see cref="File.AppendText(string)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="relativePath"></param>
        /// <param name="pathID"></param>
        /// <returns></returns>
        public static StreamWriter AppendText(this IFileSystem self, string relativePath, string pathID = null)
        {
            return new StreamWriter(self.Open(relativePath, FileMode.Append, FileAccess.Write, FileShare.Read, pathID));
        }
    }
}
