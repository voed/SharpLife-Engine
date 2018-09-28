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
using Lidgren.Network;
using Serilog;
using SharpLife.Networking.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.Messages
{
    /// <summary>
    /// Provides functionality to receive messages
    /// TODO: augment with support for user messages
    /// </summary>
    public sealed class MessagesReceiveHandler
    {
        private delegate void Handler(NetConnection connection, IMessage message);

#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
        private static readonly Handler DefaultHandler = (_, message) => throw new NotImplementedException($"The handler for {message.GetType().FullName} has not been registered");
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.

        private sealed class MessageHandlerData
        {
            public Handler Handler = DefaultHandler;

            public MessageDescriptor MessageDescriptor;
        }

        private readonly ILogger _logger;

        private readonly IReadOnlyList<MessageHandlerData> _messageHandlers;

        //TODO: make this atomic if accessed from another thread
        public bool TraceMessageLogging { get; set; }

        /// <summary>
        /// Creates a new messages receive handler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="messageDescriptors"></param>
        /// <param name="traceMessageLogging">Whether to enable message reception trace logging</param>
        public MessagesReceiveHandler(ILogger logger, IReadOnlyList<MessageDescriptor> messageDescriptors, bool traceMessageLogging)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (messageDescriptors == null)
            {
                throw new ArgumentNullException(nameof(messageDescriptors));
            }

            _messageHandlers = messageDescriptors.Select(messageDescriptor => new MessageHandlerData { MessageDescriptor = messageDescriptor }).ToList();

            TraceMessageLogging = traceMessageLogging;
        }

        private MessageHandlerData FindMessageData(Type type)
        {
            foreach (var messageHandlerData in _messageHandlers)
            {
                if (messageHandlerData.MessageDescriptor.ClrType == type)
                {
                    return messageHandlerData;
                }
            }

            return null;
        }

        /// <summary>
        /// Registers a handler for the specified message type
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="handler"></param>
        public void RegisterHandler<TMessage>(IMessageReceiveHandler<TMessage> handler)
            where TMessage : class, IMessage
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var type = typeof(TMessage);

            var data = FindMessageData(type);

            if (data == null)
            {
                //If you get this exception, make sure to add the message type to the handler
                throw new InvalidOperationException($"Message type {type.FullName} has not been registered in the messages receive handler and handlers cannot be registered for it");
            }

            if (data.Handler != DefaultHandler)
            {
                throw new InvalidOperationException($"Message type {type.FullName} already has a handler registered for it");
            }

            data.Handler = (connection, message) => handler.ReceiveMessage(connection, message as TMessage);
        }

        private MessageHandlerData LookupMessageHandlerData(uint messageId)
        {
            if (messageId >= _messageHandlers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(messageId), $"Message id {messageId} is invalid");
            }

            return _messageHandlers[(int)messageId];
        }

        private IMessage ReadDelimitedMessage(NetBufferStream stream, MessageDescriptor messageDescriptor)
        {
            //Deserialize the Protobuf message
            return messageDescriptor.Parser.ParseDelimitedFrom(stream);
        }

        private MessagesList ReadMessagesList(NetBufferStream stream)
        {
            return MessagesList.Parser.ParseDelimitedFrom(stream);
        }

        /// <summary>
        /// Reads messages from the stream and dispatches them to their registered handlers in the order that they are encountered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void ReadMessages(NetConnection sender, NetIncomingMessage message)
        {
            //Sender is passed separately into this method because remote hails don't have a sender
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            using (var stream = new NetBufferStream(message))
            {
                var list = ReadMessagesList(stream);

                for (var i = 0; i < list.MessageIds.Count; ++i)
                {
                    var data = LookupMessageHandlerData(list.MessageIds[i]);

                    var protobufMessage = ReadDelimitedMessage(stream, data.MessageDescriptor);

                    if (TraceMessageLogging)
                    {
                        _logger.Verbose($"Received message {protobufMessage.GetType().Name} from {sender.RemoteEndPoint}");
                    }

                    data.Handler(sender, protobufMessage);
                }
            }
        }
    }
}
