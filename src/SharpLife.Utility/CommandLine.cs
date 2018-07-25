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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Utility
{
    public class CommandLine : ICommandLine
    {
        private readonly List<string> _arguments;

        private readonly List<string> _keyPrefixes;

        public string this[int index] => _arguments[index];

        public int Count => _arguments.Count;

        /// <summary>
        /// Creates a new command line wrapper
        /// </summary>
        /// <param name="arguments">Command line arguments to wrap</param>
        /// <param name="keyPrefixes">If not null, the prefixes for keys to determine if a key has an associated value</param>
        public CommandLine(IReadOnlyList<string> arguments, IReadOnlyList<string> keyPrefixes = null)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            _arguments = arguments.ToList();

            _keyPrefixes = keyPrefixes?.ToList() ?? new List<string>();
        }

        public bool Contains(string name) => _arguments.Contains(name);

        public bool ContainsValue(string key) => GetValue(key) != null;

        public int IndexOf(string name) => _arguments.IndexOf(name);

        public string GetValue(string key)
        {
            return GetValue(key, _keyPrefixes);
        }

        public bool TryGetValue(string key, out string value)
        {
            value = GetValue(key);

            return value != null;
        }

        public string GetValue(string key, IEnumerable<string> keyPrefixes)
        {
            if (keyPrefixes == null)
            {
                throw new ArgumentNullException(nameof(keyPrefixes));
            }

            var index = IndexOf(key);

            if (index != -1 && index + 1 < _arguments.Count)
            {
                var value = _arguments[index + 1];

                //Check if the value starts with a key prefix
                //If so, it isn't a value
                if (keyPrefixes.All(prefix => !value.StartsWith(prefix)))
                {
                    return value;
                }
            }

            return null;
        }

        public IList<string> GetValues(string key)
        {
            return GetValues(key, _keyPrefixes);
        }

        public IList<string> GetValues(string key, IEnumerable<string> keyPrefixes)
        {
            if (keyPrefixes == null)
            {
                throw new ArgumentNullException(nameof(keyPrefixes));
            }

            var index = IndexOf(key);

            if (index != -1 && index + 1 < _arguments.Count)
            {
                var list = new List<string>();

                for (var nextValue = index + 1; nextValue < _arguments.Count; ++nextValue)
                {
                    var value = _arguments[nextValue];

                    if (keyPrefixes.Any(value.StartsWith))
                    {
                        break;
                    }

                    list.Add(value);
                }

                return list;
            }

            return null;
        }

        public IEnumerator<string> GetEnumerator() => _arguments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
