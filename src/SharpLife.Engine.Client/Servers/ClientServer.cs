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
using SharpLife.Networking.Shared.Communication.Messages;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Servers
{
    /// <summary>
    /// Represents a server that the client is connected to
    /// </summary>
    internal sealed class ClientServer
    {
        /// <summary>
        /// Name of the server being connected to
        /// May contain a port value
        /// </summary>
        public string Name { get; }

        public NetConnection Connection { get; }

        /// <summary>
        /// Our IP address as reported by the server
        /// </summary>
        public IPEndPoint TrueAddress { get; set; } = new IPEndPoint(IPAddress.None, 0);

        private readonly PendingMessages _pendingMessages;

        public ClientServer(SendMappings sendMappings, string name, NetConnection connection)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _pendingMessages = new PendingMessages(sendMappings ?? throw new ArgumentNullException(nameof(sendMappings)));
        }

        private PendingMessages GetMessages()
        {
            return _pendingMessages;
        }

        public bool HasPendingMessages()
        {
            return GetMessages().MessageCount > 0;
        }

        public void AddMessage(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            GetMessages().Add(message);
        }

        /// <summary>
        /// Writes all pending messages to the outgoing message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="reliable"></param>
        public void WriteMessages(NetOutgoingMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            GetMessages().Write(message);
        }
    }
}
