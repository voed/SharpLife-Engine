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

using Google.Protobuf.Reflection;
using SharpLife.Networking.Shared.Messages.BinaryData;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.BinaryData
{
    public sealed class BinaryDataTransmissionDescriptorSet : IBinaryDataDescriptorSet
    {
        private readonly IReadOnlyDictionary<MessageDescriptor, uint> _descriptorToIndexMap;

        internal BinaryDataTransmissionDescriptorSet(IReadOnlyDictionary<MessageDescriptor, uint> descriptorToIndexMap)
        {
            _descriptorToIndexMap = descriptorToIndexMap ?? throw new ArgumentNullException(nameof(descriptorToIndexMap));
        }

        public bool Contains(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return _descriptorToIndexMap.ContainsKey(descriptor);
        }

        public uint GetDescriptorIndex(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return _descriptorToIndexMap[descriptor];
        }

        public List<BinaryMetaData> CreateBinaryTypesMessages()
        {
            var list = new List<BinaryMetaData>(_descriptorToIndexMap.Count);

            foreach (var descriptor in _descriptorToIndexMap)
            {
                list.Add(new BinaryMetaData
                {
                    Index = descriptor.Value,
                    TypeName = descriptor.Key.ClrType.FullName
                });
            }

            return list;
        }
    }
}
