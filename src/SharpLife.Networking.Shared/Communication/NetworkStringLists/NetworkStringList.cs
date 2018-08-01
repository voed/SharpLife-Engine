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

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    internal sealed class NetworkStringList : INetworkStringList
    {
        private readonly List<string> _list = new List<string>();

        public string Name { get; }

        internal int Index { get; }

        public int Count => _list.Count;

        public string this[int index] => _list[index];

        public event Action<IReadOnlyNetworkStringList, int> OnStringAdded;

        public NetworkStringList(string name, int index)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("String list name must be valid", nameof(name));
            }

            Name = name;

            Index = index;
        }

        public int IndexOf(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return _list.IndexOf(value);
        }

        public int Add(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var index = _list.IndexOf(value);

            if (index == -1)
            {
                _list.Add(value);

                index = _list.Count - 1;

                OnStringAdded?.Invoke(this, index);
            }

            return index;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal void Clear()
        {
            _list.Clear();
            _list.TrimExcess();
        }
    }
}
