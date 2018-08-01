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
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.MessageMapping;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Clients
{
    /// <summary>
    /// Represents a single client on a server
    /// </summary>
    public sealed class ServerClient
    {
        private readonly SendMappings _sendMappings;

        public NetConnection Connection { get; }

        public IPEndPoint RemoteEndPoint => Connection?.RemoteEndPoint;

        /// <summary>
        /// This client's index
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The client's user id
        /// </summary>
        public int UserId { get; }

        /// <summary>
        /// Whether this is a fake client (server only bot)
        /// </summary>
        public bool IsFakeClient { get; }

        public bool Connected { get; set; }

        public double ConnectionStarted { get; set; }

        public ServerClientSetupStage SetupStage { get; set; }

        /// <summary>
        /// Whether the client has spawned in the world
        /// </summary>
        public bool Spawned { get; set; }

        /// <summary>
        /// Whether the client is active
        /// </summary>
        public bool Active { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Id of the last string list that did a full update
        /// </summary>
        public int LastStringListFullUpdate { get; set; } = -1;

        /// <summary>
        /// Next string list to do a full update for
        /// </summary>
        public int NextStringListToSend { get; set; } = -1;

        private readonly PendingMessages _reliableMessages;

        private readonly PendingMessages _unreliableMessages;

        private ServerClient(SendMappings sendMappings, NetConnection connection, int index, int userId, string name)
        {
            _sendMappings = sendMappings ?? throw new ArgumentNullException(nameof(sendMappings));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Index = index;
            UserId = userId;
            Name = name ?? throw new ArgumentNullException(nameof(name));

            _reliableMessages = new PendingMessages(_sendMappings);
            _unreliableMessages = new PendingMessages(_sendMappings);
        }

        /// <summary>
        /// Creates a fake client
        /// </summary>
        /// <param name="index"></param>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        private ServerClient(int index, int userId, string name)
        {
            IsFakeClient = true;
            Index = index;
            UserId = userId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static ServerClient CreateClient(SendMappings sendMappings, NetConnection connection, int index, int userId, string name)
        {
            return new ServerClient(sendMappings, connection, index, userId, name);
        }

        /// <summary>
        /// Creates a fake client that will behave like a client on the server, but has no network connection
        /// </summary>
        /// <param name="index"></param>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ServerClient CreateFakeClient(int index, int userId, string name)
        {
            return new ServerClient(index, userId, name);
        }

        public void Disconnect(string reason)
        {
            if (reason == null)
            {
                throw new ArgumentNullException(nameof(reason));
            }

            if (!IsFakeClient)
            {
                Connection.Disconnect(reason);
            }
        }

        private PendingMessages GetMessages(bool reliable)
        {
            return reliable ? _reliableMessages : _unreliableMessages;
        }

        public bool HasPendingMessages(bool reliable)
        {
            return GetMessages(reliable).MessageCount > 0;
        }

        public void AddMessage(IMessage message, bool reliable)
        {
            if (IsFakeClient)
            {
                return;
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            GetMessages(reliable).Add(message);
        }

        /// <summary>
        /// Writes all pending messages to the outgoing message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="reliable"></param>
        public void WriteMessages(NetOutgoingMessage message, bool reliable)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            GetMessages(reliable).Write(message);
        }
    }
}
