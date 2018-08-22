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
using Lidgren.Network;
using SharpLife.Networking.Shared.Messages;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpLife.Networking.Shared.Communication.Messages
{
    /// <summary>
    /// Contains a list of messages that are pending transmission
    /// This is an efficient way to store messages without needing to store the actual objects
    /// </summary>
    public sealed class PendingMessages
    {
        private readonly SendMappings _sendMappings;

        private readonly MessagesList _list = new MessagesList();

        private readonly MemoryStream _data = new MemoryStream();

        /// <summary>
        /// The number of messages that are pending transmission
        /// </summary>
        public int MessageCount => _list.MessageIds.Count;

        public PendingMessages(SendMappings sendMappings)
        {
            _sendMappings = sendMappings ?? throw new ArgumentNullException(nameof(sendMappings));
        }

        /// <summary>
        /// Adds a message to the list
        /// </summary>
        /// <param name="message"></param>
        public void Add(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _list.MessageIds.Add(_sendMappings.GetId(message));

            message.WriteDelimitedTo(_data);
        }

        /// <summary>
        /// Adds all of the messages in the given enumerable
        /// </summary>
        /// <param name="messages"></param>
        public void AddRange(IEnumerable<IMessage> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            foreach (var message in messages)
            {
                Add(message);
            }
        }

        private void InternalWrite(NetBufferStream stream)
        {
            _list.WriteDelimitedTo(stream);

            if (_data.Length > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("Message data is too large to send");
            }

            stream.Write(_data.GetBuffer(), 0, (int)_data.Length);

            Clear();
        }

        /// <summary>
        /// Writes all messages to the given stream
        /// </summary>
        /// <param name="stream"></param>
        public void Write(NetBufferStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            InternalWrite(stream);
        }

        /// <summary>
        /// Writes all messages to the given message
        /// </summary>
        /// <param name="message"></param>
        public void Write(NetOutgoingMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            using (var stream = new NetBufferStream(message))
            {
                InternalWrite(stream);
            }
        }

        /// <summary>
        /// Clears all pending messages
        /// </summary>
        public void Clear()
        {
            _list.MessageIds.Clear();
            _data.SetLength(0);
        }
    }
}
