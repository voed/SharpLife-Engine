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
using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.Server.Clients;
using SharpLife.Engine.Server.Host;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Communication.Messages;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;
using SharpLife.Utility;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Networking
{
    internal sealed class NetworkServer : NetworkPeer,
        IMessageReceiveHandler<SendResources>
    {
        private readonly ILogger _logger;

        private readonly EngineServerHost _serverHost;

        private readonly IServerNetworkListener _listener;

        private readonly SendMappings _sendMappings;

        private readonly MessagesReceiveHandler _receiveHandler;

        private readonly BinaryDataTransmissionDescriptorSet _binaryDataDescriptorSet;

        private readonly TypeRegistry _objectListTypeRegistry;

        private readonly ITime _engineTime;

        private readonly NetServer _server;

        private int _nextUserId = 1;

        protected override NetPeer Peer => _server;

        public bool IsRunning => _server.Status == NetPeerStatus.Running;

        public ServerClientList ClientList { get; }

        public bool TraceMessageLogging
        {
            get => _receiveHandler.TraceMessageLogging;
            set => _receiveHandler.TraceMessageLogging = value;
        }

        //Map specific instances
        private NetworkStringListTransmitter _stringListTransmitter;

        private NetworkObjectListTransmitter _objectListTransmitter;

        /// <summary>
        /// Creates a new server network handler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serverHost"></param>
        /// <param name="listener"></param>
        /// <param name="sendMappings"></param>
        /// <param name="receiveHandler"></param>
        /// <param name="binaryDataDescriptorSet"></param>
        /// <param name="objectListTypeRegistry"></param>
        /// <param name="engineTime"></param>
        /// <param name="maxPlayers"></param>
        /// <param name="appIdentifier"></param>
        /// <param name="ipAddress"></param>
        /// <param name="maxClients"></param>
        /// <param name="connectionTimeout"></param>
        public NetworkServer(ILogger logger,
            EngineServerHost serverHost,
            IServerNetworkListener listener,
            SendMappings sendMappings,
            MessagesReceiveHandler receiveHandler,
            BinaryDataTransmissionDescriptorSet binaryDataDescriptorSet,
            TypeRegistry objectListTypeRegistry,
            ITime engineTime,
            IVariable maxPlayers,
            string appIdentifier,
            IPEndPoint ipAddress,
            int maxClients,
            float connectionTimeout)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serverHost = serverHost ?? throw new ArgumentNullException(nameof(serverHost));
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _sendMappings = sendMappings ?? throw new ArgumentNullException(nameof(sendMappings));
            _receiveHandler = receiveHandler ?? throw new ArgumentNullException(nameof(receiveHandler));
            _binaryDataDescriptorSet = binaryDataDescriptorSet ?? throw new ArgumentNullException(nameof(binaryDataDescriptorSet));
            _objectListTypeRegistry = objectListTypeRegistry ?? throw new ArgumentNullException(nameof(objectListTypeRegistry));

            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));

            if (maxPlayers == null)
            {
                throw new ArgumentNullException(nameof(maxPlayers));
            }

            ClientList = new ServerClientList(NetConstants.MaxClients, maxPlayers);

            //Register our handlers
            _receiveHandler.RegisterHandler<SendResources>(this);

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

        protected override void HandlePacket(NetIncomingMessage message)
        {
            //TODO: filter packets

            switch (message.MessageType)
            {
                case NetIncomingMessageType.Error:
                    _logger.Error("An unknown error occurred");
                    break;

                case NetIncomingMessageType.StatusChanged:
                    HandleStatusChanged(message);
                    break;

                case NetIncomingMessageType.UnconnectedData:
                    //TODO: implement
                    //RCON or query
                    break;

                case NetIncomingMessageType.ConnectionApproval:
                    HandleConnectionApproval(message);
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

            _logger.Verbose($"Client status changed: {status}");

            //Since we look up the client below we have to make sure only those states that we care about are handled
            //Some states will occur before the client is given a slot, so don't process those
            var ignore = true;

            switch (status)
            {
                case NetConnectionStatus.Connected:
                case NetConnectionStatus.Disconnecting:
                case NetConnectionStatus.Disconnected:
                    ignore = false;
                    break;
            }

            if (ignore)
            {
                return;
            }

            string reason = message.ReadString();

            var client = FindClient(message.SenderEndPoint);

            if (client != null)
            {
                switch (status)
                {
                    case NetConnectionStatus.Connected:
                        {
                            client.SetupStage = ServerClientSetupStage.AwaitingSetupStart;

                            client.Connected = true;

                            var connectAcknowledgement = new ConnectAcknowledgement
                            {
                                UserId = client.UserId,

                                //TODO: get from settings
                                IsSecure = false,

                                //TODO: define the build number
                                BuildNumber = 0
                            };

                            var clientEndPoint = client.RemoteEndPoint;

                            connectAcknowledgement.TrueAddress = clientEndPoint.Address == IPAddress.Loopback ? NetConstants.Loopback : clientEndPoint.ToString();

                            client.AddMessage(connectAcknowledgement, true);
                            break;
                        }

                    case NetConnectionStatus.Disconnecting:
                        client.Connected = false;
                        break;

                    case NetConnectionStatus.Disconnected:
                        ClientList.RemoveClient(client);
                        break;
                }
            }
        }

        private void HandleConnectionApproval(NetIncomingMessage message)
        {
            _logger.Verbose("Client begin approval");

            if (!_serverHost.Active)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedInactive);
                _logger.Verbose("Client denied: server is inactive");
                return;
            }

            //TODO: implement
            //Query IP ban list, other things

            //Check if there is a slot to put the client in
            var slot = ClientList.FindEmptySlot();

            if (slot == -1)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedNoFreeSlots);
                _logger.Verbose("Client denied: no free slots");
                return;
            }

            var protocolVersion = message.ReadVariableUInt32();

            if (protocolVersion < NetConstants.ProtocolVersion)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedProtocolVersionOlder);
                _logger.Verbose("Client denied: protocol version older");
                return;
            }
            else if (protocolVersion > NetConstants.ProtocolVersion)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedProtocolVersionNewer);
                _logger.Verbose("Client denied: protocol version newer");
                return;
            }

            using (var stream = new NetBufferStream(message))
            {
                var userInfo = ClientUserInfo.Parser.ParseDelimitedFrom(stream);

                //TODO: validate input data

                var name = userInfo.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "unnamed";
                }

                var client = ServerClient.CreateClient(_sendMappings, message.SenderConnection, _engineTime, _objectListTransmitter, slot, _nextUserId++, name);

                ClientList.AddClientToSlot(client);

                message.SenderConnection.Approve();

                client.SetupStage = ServerClientSetupStage.Connecting;
            }

            _logger.Verbose("Client approved");
        }

        private void HandleData(NetIncomingMessage message)
        {
            //Don't process data when inactive
            if (!_serverHost.Active)
            {
                return;
            }

            _receiveHandler.ReadMessages(message.SenderConnection, message);
        }

        public ServerClient FindClient(IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            var client = ClientList.FindClientByEndPoint(endPoint, false);

            if (client != null)
            {
                return client;
            }

            _logger.Warning($"Client with IP {endPoint} has no associated client instance");

            return null;
        }

        public void OnNewMapStarted()
        {
            var networkStringListBuilder = new NetworkStringListTransmitterBuilder(_binaryDataDescriptorSet);

            _listener.CreateNetworkStringLists(networkStringListBuilder);

            _stringListTransmitter = networkStringListBuilder.Build();

            //TODO: need to define number of frames for multiplayer
            _objectListTransmitter = new NetworkObjectListTransmitter(_objectListTypeRegistry, 8);

            using (var networkObjectListBuilder = new NetworkObjectListTransmitterBuilder(_objectListTransmitter))
            {
                _listener.CreateNetworkObjectLists(networkObjectListBuilder);
            }
        }

        /// <summary>
        /// Sends all pending messages for the given client
        /// </summary>
        /// <param name="client"></param>
        private void SendClientMessages(ServerClient client)
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

        public void DropClient(ServerClient client, string reason)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (reason == null)
            {
                throw new ArgumentNullException(nameof(reason));
            }

            //TODO: notify game

            _logger.Information($"Dropped {client.Name} from server\nReason:  {reason}");

            if (!client.IsFakeClient)
            {
                client.Connection.Disconnect(reason);
                _objectListTransmitter.DestroyTransmitter(client.FrameListTransmitter);
            }
        }

        private void SendStringListFullUpdate(ServerClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (client.NextStringListToSend < _stringListTransmitter.Count)
            {
                var update = _stringListTransmitter.CreateFullUpdate(client.NextStringListToSend);

                client.AddMessage(update, true);

                client.LastStringListFullUpdate = client.NextStringListToSend;

                //Await client confirmation before sending next list
                //Don't call ContinueSetup here, it will set up the next list before the client has confirmed receipt
            }
            else
            {
                ContinueSetup(client);
            }

            client.NextStringListToSend = -1;
        }

        /// <summary>
        /// Send string list updates to all connected clients
        /// </summary>
        private void SendStringListUpdates()
        {
            //Only go through these steps if there are lists to send
            var updates = _stringListTransmitter.CreateUpdates();

            foreach (var client in ClientList)
            {
                //Only if we've reached the point where we're sending string lists
                if (client.Connected && client.SetupStage >= ServerClientSetupStage.SendingStringLists)
                {
                    var lastIdWeCanUpdate = client.LastStringListFullUpdate;

                    if (client.SetupStage == ServerClientSetupStage.SendingStringLists
                        && client.NextStringListToSend != -1)
                    {
                        SendStringListFullUpdate(client);
                    }

                    foreach (var update in updates)
                    {
                        //If we're sending full updates, we only want the client to receive these updates if they already received the initial full update
                        //Since the last sent full update occurs just before these updates,
                        //we don't send the data since anything this update sends is already part of the full update
                        if ((int)update.ListId <= lastIdWeCanUpdate)
                        {
                            client.AddMessage(update, true);
                        }
                    }
                }
            }
        }

        private void SendObjectListUpdates()
        {
            _objectListTransmitter.CreateFramesForTransmitters();

            foreach (var client in ClientList)
            {
                if (client.CanTransmit)
                {
                    client.SendObjectListFrames();
                }
            }
        }

        private void SendObjectListListMetaData(ServerClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            client.AddMessage(_objectListTransmitter.SerializeListMetaData(), true);
        }

        public void RunFrame()
        {
            SendStringListUpdates();
            SendObjectListUpdates();

            foreach (var client in ClientList)
            {
                SendClientMessages(client);
            }
        }

        /// <summary>
        /// Client requests for resources during setup will send data in sequence
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        public void ReceiveMessage(NetConnection connection, SendResources message)
        {
            var client = ClientList.FindClientByEndPoint(connection.RemoteEndPoint);

            ContinueSetup(client);
        }

        /// <summary>
        /// Continue setting up the client
        /// </summary>
        /// <param name="client"></param>
        private void ContinueSetup(ServerClient client)
        {
            switch (client.SetupStage)
            {
                case ServerClientSetupStage.AwaitingResourceTransmissionStart:
                    {
                        client.SetupStage = ServerClientSetupStage.SendingStringListsBinaryMetaData;

                        client.AddMessages(_binaryDataDescriptorSet.CreateBinaryTypesMessages(), true);
                        break;
                    }

                case ServerClientSetupStage.SendingStringListsBinaryMetaData:
                    {
                        client.SetupStage = ServerClientSetupStage.SendingStringLists;

                        client.NextStringListToSend = 0;
                        break;
                    }

                case ServerClientSetupStage.SendingStringLists:
                    {
                        //This is a list by list update so this needs to account for where we are
                        if (client.NextStringListToSend < _stringListTransmitter.Count)
                        {
                            client.NextStringListToSend = client.LastStringListFullUpdate + 1;
                        }
                        else
                        {
                            client.AddMessage(new NetworkStringListFullUpdatesComplete(), true);

                            client.SetupStage = ServerClientSetupStage.SendingObjectListTypeMetaData;

                            client.AddMessage(_objectListTransmitter.TypeRegistry.Serialize(), true);
                        }

                        break;
                    }

                case ServerClientSetupStage.SendingObjectListTypeMetaData:
                    {
                        client.SetupStage = ServerClientSetupStage.SendingObjectListListMetaData;

                        SendObjectListListMetaData(client);
                        break;
                    }

                case ServerClientSetupStage.SendingObjectListListMetaData:
                    {
                        //TODO: set next stage
                        client.SetupStage = ServerClientSetupStage.Connected;
                        break;
                    }

                default:
                    {
                        _logger.Error($"Client requested sending of resources while in invalid state {client.SetupStage}");
                        break;
                    }
            }
        }
    }
}
