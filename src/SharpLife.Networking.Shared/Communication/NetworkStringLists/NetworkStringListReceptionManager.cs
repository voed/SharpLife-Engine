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
using Google.Protobuf.Reflection;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public sealed class NetworkStringListReceptionManager : IBinaryDataDescriptorSet
    {
        private readonly NetworkStringListManager _listManager;

        private readonly Dictionary<uint, NetworkStringList> _idToListMap = new Dictionary<uint, NetworkStringList>();

        private readonly List<MessageDescriptor> _binaryDescriptors = new List<MessageDescriptor>();

        private readonly Dictionary<uint, MessageDescriptor> _binaryIndexToDescriptor = new Dictionary<uint, MessageDescriptor>();

        public int Count => _listManager.Count;

        public IBinaryDataDescriptorSet BinaryDataDescriptorSet => this;

        public NetworkStringListReceptionManager()
        {
            _listManager = new NetworkStringListManager(this);
        }

        public IReadOnlyNetworkStringList CreateList(string name)
        {
            return _listManager.CreateList(name);
        }

        public void Clear()
        {
            _binaryDescriptors.Clear();
            _binaryIndexToDescriptor.Clear();
            _idToListMap.Clear();
            _listManager.Clear();
        }

        bool IBinaryDataDescriptorSet.Contains(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return _binaryDescriptors.Contains(descriptor);
        }

        void IBinaryDataDescriptorSet.Add(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (_binaryDescriptors.Contains(descriptor))
            {
                return;
            }

            _binaryDescriptors.Add(descriptor);
        }

        public void ProcessBinaryMetaData(NetworkStringListBinaryMetaData metaData)
        {
            if (metaData == null)
            {
                throw new ArgumentNullException(nameof(metaData));
            }

            var descriptor = _binaryDescriptors.Find(desc => desc.ClrType.FullName == metaData.TypeName);

            if (descriptor == null)
            {
                throw new InvalidOperationException($"String list binary data descriptor for type {metaData.TypeName} has not been registered");
            }

            _binaryIndexToDescriptor.Add(metaData.Index, descriptor);
        }

        private IMessage ParseBinaryData(ListBinaryData binaryData)
        {
            if (binaryData.BinaryData.IsEmpty)
            {
                return null;
            }

            var descriptor = _binaryIndexToDescriptor[binaryData.DataType];

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
