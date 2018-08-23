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
using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.Client.Host;
using SharpLife.Engine.Client.Servers;
using SharpLife.Engine.Shared.Events;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Communication.Messages;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Networking
{
    /// <summary>
    /// Lidgren client networking handler
    /// </summary>
    internal class NetworkClient : NetworkPeer,
        IMessageReceiveHandler<NetworkStringListFullUpdate>,
        IMessageReceiveHandler<NetworkStringListUpdate>,
        IMessageReceiveHandler<NetworkObjectListFrameListUpdate>,
        IMessageReceiveHandler<NetworkObjectListListMetaDataList>
    {
        /// <summary>
        /// The current connection status, based on last processed status change message
        /// </summary>
        public NetConnectionStatus ConnectionStatus { get; private set; }

        public bool IsConnected => ConnectionStatus != NetConnectionStatus.None && ConnectionStatus != NetConnectionStatus.Disconnected;

        public bool IsDisconnecting { get; private set; }

        private readonly ILogger _logger;

        private readonly EngineClientHost _clientHost;

        private readonly SendMappings _sendMappings;

        private readonly MessagesReceiveHandler _receiveHandler;

        private readonly IFrameListReceiverListener _frameListReceiverListener;

        private readonly IVariable _cl_name;

        private readonly NetClient _client;

        protected override NetPeer Peer => _client;

        public ClientServer Server { get; private set; }

        public NetworkStringListReceiver StringListReceiver { get; }

        public NetworkObjectListReceiver ObjectListReceiver { get; private set; }

        /// <summary>
        /// Invoked when the client has fully disconnected from a server
        /// </summary>
        public event Action OnFullyDisconnected;

        private bool _doFullDisconnectCallback;

        /// <summary>
        /// Creates a new client network handler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="clientHost"></param>
        /// <param name="sendMappings"></param>
        /// <param name="receiveHandler"></param>
        /// <param name="binaryDescriptorSet"></param>
        /// <param name="frameListReceiverListener"></param>
        /// <param name="cl_name"></param>
        /// <param name="appIdentifier">App identifier to use for networking. Must match the identifier given to servers</param>
        /// <param name="port">Port to use</param>
        /// <param name="resendHandshakeInterval"></param>
        /// <param name="connectionTimeout"></param>
        public NetworkClient(ILogger logger,
            EngineClientHost clientHost,
            SendMappings sendMappings,
            MessagesReceiveHandler receiveHandler,
            BinaryDataReceptionDescriptorSet binaryDescriptorSet,
            IFrameListReceiverListener frameListReceiverListener,
            IVariable cl_name,
            string appIdentifier,
            int port,
            float resendHandshakeInterval,
            float connectionTimeout)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientHost = clientHost ?? throw new ArgumentNullException(nameof(clientHost));
            _sendMappings = sendMappings ?? throw new ArgumentNullException(nameof(sendMappings));
            _receiveHandler = receiveHandler ?? throw new ArgumentNullException(nameof(receiveHandler));

            if (binaryDescriptorSet == null)
            {
                throw new ArgumentNullException(nameof(binaryDescriptorSet));
            }

            StringListReceiver = new NetworkStringListReceiver(binaryDescriptorSet);

            _frameListReceiverListener = frameListReceiverListener ?? throw new ArgumentNullException(nameof(frameListReceiverListener));
            _cl_name = cl_name ?? throw new ArgumentNullException(nameof(cl_name));

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

        public void RunFrame()
        {
            //This is done here to ensure that we've got no more packets to read
            if (_doFullDisconnectCallback)
            {
                _doFullDisconnectCallback = false;

                OnFullyDisconnected?.Invoke();
                OnFullyDisconnected = null;
            }
        }

        public void Connect(string address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (_clientHost.ConnectionSetupStatus == ClientConnectionSetupStatus.Connecting)
            {
                _logger.Information($"Already connecting to a server; connect command to {address} ignored");
                return;
            }

            if (IsConnected)
            {
                //Disconnect from current server first
                if (!IsDisconnecting)
                {
                    Disconnect(NetMessages.ClientDisconnectMessage);
                }

                //Delay until we've fully disconnected
                OnFullyDisconnected += () => Connect(address);
                return;
            }

            _clientHost.EventSystem.DispatchEvent(EngineEvents.ClientStartConnect);

            //Told to connect to listen server, translate address
            if (address == NetAddresses.Local)
            {
                address = NetConstants.LocalHost;
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

            var userInfo = new ClientUserInfo
            {
                Name = _cl_name.String
            };

            using (var stream = new NetBufferStream(message))
            {
                userInfo.WriteDelimitedTo(stream);
            }

            var connection = _client.Connect(ipAddress, message);

            if (connection == null)
            {
                //Already started connecting, but status hasn't updated yet
                //Should always be caught by the setup status check above
                _logger.Debug("Lidgren returned null connection from Connect()");
                return;
            }

            _clientHost.ConnectionSetupStatus = ClientConnectionSetupStatus.Connecting;

            Server = new ClientServer(_sendMappings, address, connection);
        }

        /// <summary>
        /// Disconnect the client from the server, if connected
        /// </summary>
        /// <param name="byeMessage"></param>
        public void Disconnect(string byeMessage)
        {
            //Don't disconnect if not connected or if already disconnecting
            if (IsConnected && !IsDisconnecting)
            {
                IsDisconnecting = true;

                _client.Disconnect(byeMessage);

                //TODO: close Steam connection

                _clientHost.EventSystem.DispatchEvent(EngineEvents.ClientDisconnectSent);
            }
        }

        protected override void HandlePacket(NetIncomingMessage message)
        {
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    HandleStatusChanged(message);
                    break;

                case NetIncomingMessageType.UnconnectedData:
                    //TODO: implement
                    break;

                case NetIncomingMessageType.Data:
                    HandleData(message);
                    break;

                case NetIncomingMessageType.VerboseDebugMessage:
                    _logger.Verbose(message.ReadString());
                    break;

                case NetIncomingMessageType.DebugMessage:
                    _logger.Debug(message.ReadString());
                    break;

                case NetIncomingMessageType.WarningMessage:
                    _logger.Warning(message.ReadString());
                    break;

                case NetIncomingMessageType.ErrorMessage:
                    _logger.Error(message.ReadString());
                    break;
            }
        }

        private void HandleStatusChanged(NetIncomingMessage message)
        {
            var status = (NetConnectionStatus)message.ReadByte();

            ConnectionStatus = status;

            string reason = message.ReadString();

            _logger.Verbose($"Server status changed: {status} {reason}");

            switch (status)
            {
                //Clear our server data only once we're fully disconnected so we can process the packets
                case NetConnectionStatus.Disconnected:
                    {
                        if (_clientHost.ConnectionSetupStatus != ClientConnectionSetupStatus.NotConnected)
                        {
                            _clientHost.ConnectionSetupStatus = ClientConnectionSetupStatus.NotConnected;

                            //Don't process disconnect initiated by us
                            if (reason != NetMessages.ClientDisconnectMessage)
                            {
                                //Disconnected by server
                                _clientHost.EndGame("Server disconnected");
                            }
                        }

                        StringListReceiver.Clear();

                        Server = null;

                        IsDisconnecting = false;
                        _doFullDisconnectCallback = true;

                        break;
                    }
            }
        }

        private void HandleData(NetIncomingMessage message)
        {
            if (_clientHost.ConnectionSetupStatus == ClientConnectionSetupStatus.NotConnected)
            {
                return;
            }

            _receiveHandler.ReadMessages(message.SenderConnection, message);
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

        public void CreateObjectListReceiver(TypeRegistry typeRegistry)
        {
            //TODO: need to define number of frames for multiplayer
            ObjectListReceiver = new NetworkObjectListReceiver(typeRegistry, 8, _frameListReceiverListener);
        }

        public void ReceiveMessage(NetConnection connection, NetworkStringListFullUpdate message)
        {
            StringListReceiver.ProcessFullUpdate(message);
        }

        public void ReceiveMessage(NetConnection connection, NetworkStringListUpdate message)
        {
            StringListReceiver.ProcessUpdate(message);
        }

        public void ReceiveMessage(NetConnection connection, NetworkObjectListFrameListUpdate message)
        {
            ObjectListReceiver.DeserializeFrameList(message);
            ObjectListReceiver.ApplyCurrentFrame();
        }

        public void ReceiveMessage(NetConnection connection, NetworkObjectListListMetaDataList message)
        {
            ObjectListReceiver.DeserializeListMetaData(message);

            _clientHost.RequestResources();
        }
    }
}
