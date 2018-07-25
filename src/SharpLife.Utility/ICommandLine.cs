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

namespace SharpLife.Utility
{
    /// <summary>
    /// Provides easy access to command line arguments and common operations on it
    /// </summary>
    public interface ICommandLine : IEnumerable<string>
    {
        /// <summary>
        /// Gets an argument by index
        /// 0 is the path to the program
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string this[int index] { get; }

        /// <summary>
        /// the number of command line arguments
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns whether the given argument is present on the command line
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Contains(string name);

        /// <summary>
        /// Given a key, returns whether there is a value for it
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool ContainsValue(string key);

        /// <summary>
        /// Gets the index of the given argument
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        int IndexOf(string name);

        /// <summary>
        /// Gets the first value for a given key, or null if the key does not exist, or if there is no value for it
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetValue(string key);

        /// <summary>
        /// <see cref="GetValue(string)"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetValue(string key, out string value);

        /// <summary>
        /// Overload to allow use of a custom key prefix list
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyPrefixes">Sequence of prefixes to check</param>
        /// <returns></returns>
        string GetValue(string key, IEnumerable<string> keyPrefixes);

        /// <summary>
        /// Gets all values for a given key, or null if the key does not exist
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IList<string> GetValues(string key);

        /// <summary>
        /// Overload to allow use of a custom key prefix list
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyPrefixes"></param>
        /// <returns></returns>
        IList<string> GetValues(string key, IEnumerable<string> keyPrefixes);
    }
}
