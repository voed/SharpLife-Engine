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

using Lidgren.Network;
using SharpLife.Engine.Server.Clients;
using SharpLife.Networking.Shared;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Networking
{
    internal sealed class NetworkServer : NetworkPeer
    {
        private readonly NetServer _server;

        protected override NetPeer Peer => _server;

        /// <summary>
        /// Creates a new server network handler
        /// </summary>
        /// <param name="appIdentifier"></param>
        /// <param name="ipAddress"></param>
        /// <param name="maxClients"></param>
        /// <param name="connectionTimeout"></param>
        public NetworkServer(string appIdentifier, IPEndPoint ipAddress, int maxClients, float connectionTimeout)
        {
            var config = new NetPeerConfiguration(appIdentifier)
            {
                AcceptIncomingConnections = true,

                //We don't use these since our data must be FIFO ordered
                SuppressUnreliableUnorderedAcks = true,

                MaximumConnections = maxClients,

                MaximumHandshakeAttempts = NetConstants.MaxHandshakeAttempts,

                LocalAddress = ipAddress.Address,

                Port = ipAddress.Port,

                ConnectionTimeout = connectionTimeout,
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _server = new NetServer(config);
        }

        /// <summary>
        /// Sends all pending messages for the given client
        /// </summary>
        /// <param name="client"></param>
        public void SendClientMessages(ServerClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (client.HasPendingMessages(true))
            {
                var reliable = CreatePacket();

                client.WriteMessages(reliable, true);

                SendPacket(reliable, client.Connection, NetDeliveryMethod.ReliableOrdered);
            }

            if (client.HasPendingMessages(false))
            {
                var unreliable = CreatePacket();

                client.WriteMessages(unreliable, false);

                SendPacket(unreliable, client.Connection, NetDeliveryMethod.UnreliableSequenced);
            }
        }
    }
}
