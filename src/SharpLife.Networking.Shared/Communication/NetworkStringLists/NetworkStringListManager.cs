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

using SharpLife.Networking.Shared.Communication.BinaryData;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public sealed class NetworkStringListManager
    {
        private readonly IBinaryDataDescriptorSet _binaryDataDescriptorSet;

        private readonly List<NetworkStringList> _stringLists = new List<NetworkStringList>();

        public int Count => _stringLists.Count;

        internal NetworkStringList this[int index]
        {
            get => _stringLists[index];
        }

        public NetworkStringListManager(IBinaryDataDescriptorSet binaryDataDescriptorSet)
        {
            _binaryDataDescriptorSet = binaryDataDescriptorSet ?? throw new ArgumentNullException(nameof(binaryDataDescriptorSet));
        }

        internal NetworkStringList FindByName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _stringLists.Find(list => list.Name == name);
        }

        public bool Contains(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _stringLists.FindIndex(list => list.Name == name) != -1;
        }

        public INetworkStringList CreateList(string name)
        {
            if (Contains(name))
            {
                throw new ArgumentException($"Cannot create string list with duplicate name {name}", nameof(name));
            }

            var list = new NetworkStringList(_binaryDataDescriptorSet, name, _stringLists.Count);

            _stringLists.Add(list);

            return list;
        }

        public void Clear()
        {
            //Clear internal data so the memory can be reclaimed
            foreach (var list in _stringLists)
            {
                list.Clear();
            }

            _stringLists.Clear();
        }
    }
}
