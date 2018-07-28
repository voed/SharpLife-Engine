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
using SharpLife.Engine.Client.Networking;
using SharpLife.Engine.Shared.Events;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication;
using SharpLife.Networking.Shared.MessageMapping;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.Server;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost : IMessageReceiveHandler<ConnectAcknowledgement>,
        IMessageReceiveHandler<ServerInfo>,
        IMessageReceiveHandler<Print>
    {
        private SendMappings _netSendMappings;
        private MessagesReceiveHandler _netReceiveHandler;

        private void CreateMessageHandlers()
        {
            _netSendMappings = new SendMappings(NetMessages.ClientToServerMessages);
            _netReceiveHandler = new MessagesReceiveHandler(NetMessages.ServerToClientMessages);
        }

        private void RegisterMessageHandlers()
        {
            _netReceiveHandler.RegisterHandler<ConnectAcknowledgement>(this);
            _netReceiveHandler.RegisterHandler<ServerInfo>(this);
            _netReceiveHandler.RegisterHandler<Print>(this);
        }

        public void ReceiveMessage(NetConnection connection, ConnectAcknowledgement message)
        {
            EventSystem.DispatchEvent(EngineEvents.ClientReceivedAck);

            if (ConnectionStatus == ClientConnectionStatus.Connected)
            {
                _logger.Debug("Duplicate connect ack. received.  Ignored.");
                return;
            }

            _userId = message.UserId;
            _netClient.Server.TrueAddress = NetUtilities.StringToIPAddress(message.TrueAddress, NetConstants.DefaultServerPort);
            _buildNumber = message.BuildNumber;

            if (message.TrueAddress != NetConstants.Loopback)
            {
                _logger.Information($"Connection accepted by {_netClient.Server.Name}");
            }
            else
            {
                _logger.Debug("Connection accepted.");
            }

            ConnectionStatus = ClientConnectionStatus.Connected;

            //TODO: set state variables

            var newConnection = new NewConnection();

            _netClient.Server.AddMessage(newConnection);
        }

        public void ReceiveMessage(NetConnection connection, ServerInfo message)
        {
            //TODO
            //TODO: this is temporary
            if (!_engine.IsDedicatedServer)
            {
                //Load the BSP file
                if (!_engine.MapManager.LoadMap(message.MapFileName))
                {
                    _logger.Error($"Couldn't load \"{message.MapFileName}\"");
                    return;
                }
            }

            _renderer.LoadBSP(_engine.MapManager.BSPFile);
        }

        public void ReceiveMessage(NetConnection connection, Print message)
        {
            _logger.Information(message.MessageContents);
        }
    }
}
