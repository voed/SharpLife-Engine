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
using Serilog;
using SharpLife.Networking.Shared;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Networking
{
    /// <summary>
    /// Lidgren client networking handler
    /// </summary>
    internal class NetworkClient
    {
        public NetConnectionStatus ConnectionStatus => _client.ConnectionStatus;

        public bool Connected => _serverConnection != null;

        private readonly ILogger _logger;

        private readonly NetClient _client;

        private NetConnection _serverConnection;

        //Always have a valid instance for this member
        private ServerData _data = new ServerData();

        /// <summary>
        /// Name of the server being connected to
        /// May contain a port value
        /// </summary>
        public string ServerName => _data.ServerName;

        /// <summary>
        /// The resolved server address
        /// </summary>
        public IPEndPoint ServerAddress => _serverConnection?.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0);

        /// <summary>
        /// Our IP address as reported by the server
        /// </summary>
        public IPEndPoint TrueAddress => _data.TrueAddress;

        /// <summary>
        /// Creates a new client network handler
        /// </summary>
        /// <param name="appIdentifier">App identifier to use for networking. Must match the identifier given to servers</param>
        /// <param name="port">Port to use</param>
        /// <param name="resendHandshakeInterval"></param>
        /// <param name="connectionTimeout"></param>
        public NetworkClient(ILogger logger, string appIdentifier, int port, float resendHandshakeInterval, float connectionTimeout)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        public void Start()
        {
            _client.Start();
        }

        public void Shutdown(string bye)
        {
            _client.Shutdown(bye);
        }

        public void Connect(string address)
        {
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

            _serverConnection = _client.Connect(ipAddress, message);

            _data.ServerName = address;
        }

        /// <summary>
        /// Disconnect the client from the server, if connected
        /// </summary>
        /// <param name="byeMessage"></param>
        public void Disconnect(string byeMessage)
        {
            _client.Disconnect(byeMessage);

            _serverConnection = null;

            _data = new ServerData();

            //TODO: close Steam connection
        }

        public void ReadPackets(Action<NetIncomingMessage> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            NetIncomingMessage im;

            while ((im = _client.ReadMessage()) != null)
            {
                handler(im);
            }
        }
    }
}
