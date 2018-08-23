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
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.BinaryData
{
    public sealed class BinaryDataSetBuilder : IBinaryDataSetBuilder
    {
        private readonly List<MessageDescriptor> _descriptors = new List<MessageDescriptor>();

        public void Add(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (_descriptors.Contains(descriptor))
            {
                throw new InvalidOperationException($"Descriptor for message type {descriptor.ClrType.FullName} has already been registered");
            }

            _descriptors.Add(descriptor);
        }

        public BinaryDataTransmissionDescriptorSet BuildTransmissionSet()
        {
            uint index = 0;
            return new BinaryDataTransmissionDescriptorSet(_descriptors.ToDictionary(key => key, _ => index++));
        }

        public BinaryDataReceptionDescriptorSet BuildReceptionSet()
        {
            //The list is copied to prevent code that holds onto the builder from modifying the lists after the fact
            return new BinaryDataReceptionDescriptorSet(_descriptors.ToArray());
        }
    }
}
