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
    public sealed class NetworkStringListReceiver : BaseNetworkStringListManager<BinaryDataReceptionDescriptorSet>
    {
        private readonly Dictionary<uint, NetworkStringList> _idToListMap = new Dictionary<uint, NetworkStringList>();

        public NetworkStringListReceiver(BinaryDataReceptionDescriptorSet descriptorSet, IReadOnlyList<NetworkStringList> lists)
            : base(descriptorSet, lists)
        {
        }

        private IMessage ParseBinaryData(ListBinaryData binaryData)
        {
            //TODO: maybe always parse even if empty?
            if (binaryData.BinaryData.IsEmpty)
            {
                return null;
            }

            var descriptor = _binaryDataDescriptorSet.GetDescriptorByIndex(binaryData.DataType);

            return descriptor.Parser.ParseFrom(binaryData.BinaryData);
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

            var list = FindByName(update.Name);

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
