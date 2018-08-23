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
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.BinaryData
{
    public sealed class BinaryDataReceptionDescriptorSet : IBinaryDataDescriptorSet
    {
        private readonly List<MessageDescriptor> _descriptors;

        private readonly Dictionary<uint, MessageDescriptor> _binaryIndexToDescriptor = new Dictionary<uint, MessageDescriptor>();

        internal BinaryDataReceptionDescriptorSet(IReadOnlyList<MessageDescriptor> descriptors)
        {
            _descriptors = (descriptors ?? throw new ArgumentNullException(nameof(descriptors))).ToList();
        }

        public bool Contains(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return _descriptors.Contains(descriptor);
        }

        public MessageDescriptor GetDescriptorByIndex(uint index)
        {
            return _descriptors[(int)index];
        }

        public void ProcessBinaryMetaData(BinaryMetaData metaData)
        {
            if (metaData == null)
            {
                throw new ArgumentNullException(nameof(metaData));
            }

            var descriptor = _descriptors.Find(desc => desc.ClrType.FullName == metaData.TypeName);

            //This doesn't use Type.GetType to prevent malicious server messages from using unregistered messages
            if (descriptor == null)
            {
                throw new InvalidOperationException($"String list binary data descriptor for type {metaData.TypeName} has not been registered");
            }

            _binaryIndexToDescriptor.Add(metaData.Index, descriptor);
        }
    }
}
