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
using Google.Protobuf.Collections;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public sealed class NetworkStringListReceptionManager
    {
        private readonly BinaryDataReceptionDescriptorSet _descriptorSet;

        private readonly NetworkStringListManager _listManager;

        private readonly Dictionary<uint, NetworkStringList> _idToListMap = new Dictionary<uint, NetworkStringList>();

        public int Count => _listManager.Count;

        public NetworkStringListReceptionManager(BinaryDataReceptionDescriptorSet descriptorSet)
        {
            _descriptorSet = descriptorSet ?? throw new ArgumentNullException(nameof(descriptorSet));
            _listManager = new NetworkStringListManager(_descriptorSet);
        }

        public IReadOnlyNetworkStringList CreateList(string name)
        {
            return _listManager.CreateList(name);
        }

        public void Clear()
        {
            _idToListMap.Clear();
            _listManager.Clear();
        }

        private IMessage ParseBinaryData(ListBinaryData binaryData)
        {
            if (binaryData.BinaryData.IsEmpty)
            {
                return null;
            }

            var descriptor = _descriptorSet.GetDescriptorByIndex(binaryData.DataType);

            var message = descriptor.Parser.ParseFrom(binaryData.BinaryData);

            return message;
        }

        private void ProcessStringData(RepeatedField<ListStringData> strings, NetworkStringList list)
        {
            foreach (var data in strings)
            {
                list.Add(data.Value, ParseBinaryData(data.BinaryData));
            }
        }

        public void ProcessFullUpdate(NetworkStringListFullUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            var list = _listManager.FindByName(update.Name);

            _idToListMap[update.ListId] = list ?? throw new InvalidOperationException($"Full update received for non-existent table \"{update.Name}\"");

            ProcessStringData(update.Strings, list);
        }

        public void ProcessUpdate(NetworkStringListUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            if (!_idToListMap.TryGetValue(update.ListId, out var list))
            {
                throw new ArgumentOutOfRangeException(nameof(update), "Update has invalid list id");
            }

            ProcessStringData(update.Strings, list);

            foreach (var data in update.Updates)
            {
                list.SetBinaryData((int)data.Index, ParseBinaryData(data.BinaryData));
            }
        }
    }
}
