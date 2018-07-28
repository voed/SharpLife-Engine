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
using Serilog;
using SharpLife.Engine.Client.Servers;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.MessageMapping;
using SharpLife.Networking.Shared.Messages.Client;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Networking
{
    /// <summary>
    /// Lidgren client networking handler
    /// </summary>
    internal class NetworkClient : NetworkPeer
    {
        public NetConnectionStatus ConnectionStatus => _client.ConnectionStatus;

        private readonly ILogger _logger;

        private readonly SendMappings _sendMappings;

        private readonly NetClient _client;

        protected override NetPeer Peer => _client;

        public ClientServer Server { get; private set; }

        /// <summary>
        /// Creates a new client network handler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="sendMappings"></param>
        /// <param name="appIdentifier">App identifier to use for networking. Must match the identifier given to servers</param>
        /// <param name="port">Port to use</param>
        /// <param name="resendHandshakeInterval"></param>
        /// <param name="connectionTimeout"></param>
        public NetworkClient(ILogger logger, SendMappings sendMappings, string appIdentifier, int port, float resendHandshakeInterval, float connectionTimeout)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sendMappings = sendMappings ?? throw new ArgumentNullException(nameof(sendMappings));

            var config = new NetPeerConfiguration(appIdentifier)
            {
                //Clients can connect to servers, nobody can connect to clients
                AcceptIncomingConnections = false,

                //We don't use these since our data must be FIFO ordered
                SuppressUnreliableUnorderedAcks = true,

                //Client can only be connected to one server at a time
                MaximumConnections = 1,

                MaximumHandshakeAttempts = NetConstants.MaxHandshakeAttempts,

                Port = port,

                ResendHandshakeInterval = resendHandshakeInterval,

                ConnectionTimeout = connectionTimeout,
            };

            _client = new NetClient(config);
        }

        public void Connect(string address, ClientUserInfo userInfo)
        {
            if (userInfo == null)
            {
                throw new ArgumentNullException(nameof(userInfo));
            }

            IPEndPoint ipAddress;

            try
            {
                ipAddress = NetUtilities.StringToIPAddress(address, NetConstants.DefaultServerPort);
            }
            catch (Exception e)
            {
                if (e is FormatException || e is ArgumentOutOfRangeException)
                {
                    _logger.Information($"Unable to resolve {address}");
                    return;
                }

                throw;
            }

            //TODO: send information about the client to the server, along with the Steam auth token

            var message = _client.CreateMessage();

            //Send protocol version first so compatibility is known
            message.WriteVariableUInt32(NetConstants.ProtocolVersion);

            using (var stream = new NetBufferStream(message))
            {
                userInfo.WriteDelimitedTo(stream);
            }

            var connection = _client.Connect(ipAddress, message);

            Server = new ClientServer(_sendMappings, address, connection);
        }

        /// <summary>
        /// Disconnect the client from the server, if connected
        /// </summary>
        /// <param name="byeMessage"></param>
        public void Disconnect(string byeMessage)
        {
            _client.Disconnect(byeMessage);

            Server = null;

            //TODO: close Steam connection
        }

        /// <summary>
        /// Sends all pending messages for the given server
        /// </summary>
        /// <param name="server"></param>
        public void SendServerMessages(ClientServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (server.HasPendingMessages())
            {
                var reliable = CreatePacket();

                server.WriteMessages(reliable);

                SendPacket(reliable, server.Connection, NetDeliveryMethod.ReliableOrdered);
            }
        }
    }
}
