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
using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.MessageMapping
{
    /// <summary>
    /// Provides a mapping of <see cref="MessageDescriptor"/> to unsigned integer ids
    /// </summary>
    public sealed class SendMappings
    {
        private readonly Dictionary<Type, uint> _typeToIndexMap;

        /// <summary>
        /// The number of registered message types
        /// </summary>
        public int Count => _typeToIndexMap.Count;

        /// <summary>
        /// Constructs a new messages send handler
        /// </summary>
        /// <param name="messageDescriptors">Ordered list of types of messages that can be sent</param>
        public SendMappings(IReadOnlyList<MessageDescriptor> messageDescriptors)
        {
            if (messageDescriptors == null)
            {
                throw new ArgumentNullException(nameof(messageDescriptors));
            }

            uint nextId = 0;

            _typeToIndexMap = messageDescriptors.ToDictionary(descriptor => descriptor.ClrType, _ => nextId++);
        }

        private uint InternalGetId(Type type)
        {
            if (!_typeToIndexMap.TryGetValue(type, out var messageId))
            {
                //If you get this exception, make sure to add the message type
                throw new InvalidOperationException($"Message type {type.FullName} has not been registered in the send mapping");
            }

            return messageId;
        }

        public uint GetId(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return InternalGetId(message.GetType());
        }

        public uint GetId(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return InternalGetId(descriptor.ClrType);
        }
    }
}
