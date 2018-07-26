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
using SharpLife.Networking.Shared;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Networking
{
    internal sealed class NetworkServer
    {
        private readonly NetServer _server;

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

        public void Start()
        {
            _server.Start();
        }

        public void Shutdown(string bye)
        {
            _server.Shutdown(bye);
        }

        public void ReadPackets(Action<NetIncomingMessage> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            NetIncomingMessage im;

            while ((im = _server.ReadMessage()) != null)
            {
                handler(im);
            }
        }

        public void FlushOutgoingPackets()
        {
            _server.FlushSendQueue();
        }
    }
}
