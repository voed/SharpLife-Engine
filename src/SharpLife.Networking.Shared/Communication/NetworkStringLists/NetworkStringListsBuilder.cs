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
    public abstract class BaseNetworkStringListsBuilder<TBinaryDescriptorSet> : INetworkStringListsBuilder
        where TBinaryDescriptorSet : class, IBinaryDataDescriptorSet
    {
        protected readonly TBinaryDescriptorSet _descriptorSet;

        protected readonly List<NetworkStringList> _lists = new List<NetworkStringList>();

        protected BaseNetworkStringListsBuilder(TBinaryDescriptorSet descriptorSet)
        {
            _descriptorSet = descriptorSet ?? throw new ArgumentNullException(nameof(descriptorSet));
        }

        public INetworkStringList CreateList(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_lists.FindIndex(stringList => stringList.Name == name) != -1)
            {
                throw new ArgumentException($"Cannot create string list with duplicate name {name}", nameof(name));
            }

            var list = new NetworkStringList(_descriptorSet, name, _lists.Count);

            _lists.Add(list);

            return list;
        }
    }

    public sealed class NetworkStringListTransmitterBuilder : BaseNetworkStringListsBuilder<BinaryDataTransmissionDescriptorSet>
    {
        public NetworkStringListTransmitterBuilder(BinaryDataTransmissionDescriptorSet descriptorSet)
            : base(descriptorSet)
        {
        }

        public NetworkStringListTransmitter Build()
        {
            return new NetworkStringListTransmitter(_descriptorSet, _lists);
        }
    }

    public sealed class NetworkStringListReceiverBuilder : BaseNetworkStringListsBuilder<BinaryDataReceptionDescriptorSet>
    {
        public NetworkStringListReceiverBuilder(BinaryDataReceptionDescriptorSet descriptorSet)
            : base(descriptorSet)
        {
        }

        public NetworkStringListReceiver Build()
        {
            return new NetworkStringListReceiver(_descriptorSet, _lists);
        }
    }
}
