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

using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    internal sealed class NetworkStringList : INetworkStringList
    {
        private sealed class StringData
        {
            public string value;
            public IMessage binaryData;
        }

        private readonly List<StringData> _list = new List<StringData>();

        public string Name { get; }

        internal int Index { get; }

        public int Count => _list.Count;

        public string this[int index] => _list[index].value;

        public event Action<IReadOnlyNetworkStringList, int> OnStringAdded;

        public event Action<IReadOnlyNetworkStringList, int> OnBinaryDataChanged;

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

            return _list.FindIndex(data => data.value == value);
        }

        public IMessage GetBinaryData(string value)
        {
            var index = IndexOf(value);

            if (index == -1)
            {
                return null;
            }

            return GetBinaryData(index);
        }

        public IMessage GetBinaryData(int index)
        {
            var data = _list[index];

            return data.binaryData;
        }

        public int Add(string value, IMessage binaryData = null)
        {
            var index = IndexOf(value);

            if (index == -1)
            {
                _list.Add(new StringData
                {
                    value = value,
                    binaryData = binaryData
                });

                index = _list.Count - 1;

                OnStringAdded?.Invoke(this, index);
            }

            return index;
        }

        public void SetBinaryData(string value, IMessage binaryData)
        {
            var index = IndexOf(value);

            if (index == -1)
            {
                throw new ArgumentOutOfRangeException($"String {value} is not present in string list {Name}");
            }

            SetBinaryData(index, binaryData);
        }

        public void SetBinaryData(int index, IMessage binaryData)
        {
            _list[index].binaryData = binaryData;

            OnBinaryDataChanged?.Invoke(this, index);
        }

        public IEnumerator<string> GetEnumerator()
        {
            foreach (var data in _list)
            {
                yield return data.value;
            }
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
