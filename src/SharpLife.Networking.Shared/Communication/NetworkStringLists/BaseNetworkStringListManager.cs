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
using SharpLife.Utility.Collections.Generic;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public abstract class BaseNetworkStringListManager<TBinaryDescriptorSet>
        where TBinaryDescriptorSet : class, IBinaryDataDescriptorSet
    {
        protected readonly TBinaryDescriptorSet _binaryDataDescriptorSet;

        private readonly IReadOnlyList<NetworkStringList> _stringLists;

        public int Count => _stringLists.Count;

        protected NetworkStringList this[int index]
        {
            get => _stringLists[index];
        }

        protected BaseNetworkStringListManager(TBinaryDescriptorSet binaryDataDescriptorSet, IReadOnlyList<NetworkStringList> lists)
        {
            _binaryDataDescriptorSet = binaryDataDescriptorSet ?? throw new ArgumentNullException(nameof(binaryDataDescriptorSet));
            _stringLists = lists ?? throw new ArgumentNullException(nameof(lists));
        }

        protected NetworkStringList FindByName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _stringLists.Find(list => list.Name == name);
        }
    }
}
