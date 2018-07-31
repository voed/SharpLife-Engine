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
using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.Client.Networking;
using SharpLife.Engine.Shared.Events;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Messages.Client;
using System;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost
    {
        private NetworkClient _netClient;

        /// <summary>
        /// (Re)creates the network client, using current client configuration values
        /// </summary>
        private void CreateNetworkClient()
        {
            //Disconnect any previous connection
            if (_netClient != null)
            {
                Disconnect(false);
            }

            //TODO: could combine the app identifier with the game name to allow concurrent game hosts
            //Not possible since the original engine launcher blocks launching multiple instances
            //Should be possible for servers though

            _netClient = new NetworkClient(
                _logger,
                _netSendMappings,
                NetConstants.AppIdentifier,
                _clientport.Integer,
                _cl_resend.Float,
                _cl_timeout.Float);
        }

        private void SetupNetworking()
        {
            CreateMessageHandlers();
            RegisterMessageHandlers();

            CreateNetworkClient();
        }

        private void HandlePacket(NetIncomingMessage message)
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

            string reason = message.ReadString();

            switch (status)
            {
                case NetConnectionStatus.Disconnected:
                    {
                        if (ConnectionStatus != ClientConnectionStatus.NotConnected)
                        {
                            //Disconnected by server
                            _engine.EndGame("Server disconnected");
                            //TODO: discard remaining incoming packets?
                        }
                        break;
                    }
            }
        }

        private void HandleData(NetIncomingMessage message)
        {
            _netReceiveHandler.ReadMessages(message.SenderConnection, message);
        }

        /// <summary>
        /// Connect to a server
        /// </summary>
        /// <param name="command"></param>
        private void Connect(ICommandArgs command)
        {
            if (command.Count == 0)
            {
                _logger.Information("usage: connect <server>");
                return;
            }

            var name = command.ArgumentsString;

            Connect(name);
        }

        public void Connect(string address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            SetupNetworking();

            EventSystem.DispatchEvent(EngineEvents.ClientStartConnect);

            //TODO: initialize client state

            CreateNetworkClient();

            _netClient.Start();

            ConnectionStatus = ClientConnectionStatus.Connecting;

            //Told to connect to listen server, translate address
            if (address == NetAddresses.Local)
            {
                address = NetConstants.LocalHost;
            }

            var userInfo = new ClientUserInfo
            {
                Name = _cl_name.String
            };

            _netClient.Connect(address, userInfo);
        }

        private void Disconnect(ICommandArgs command)
        {
            Disconnect(true);
        }

        public void Disconnect(bool shutdownServer)
        {
            //Always dispatch even if we're not connected
            EventSystem.DispatchEvent(EngineEvents.ClientStartDisconnect);

            if (ConnectionStatus != ClientConnectionStatus.NotConnected)
            {
                //TODO: implement
                _netClient.Shutdown(NetMessages.ClientDisconnectMessage);

                //The client considers itself disconnected immediately
                ConnectionStatus = ClientConnectionStatus.NotConnected;

                EventSystem.DispatchEvent(EngineEvents.ClientDisconnectSent);
            }

            EventSystem.DispatchEvent(EngineEvents.ClientEndDisconnect);

            if (shutdownServer)
            {
                _engine.StopServer();
            }
        }
    }
}
