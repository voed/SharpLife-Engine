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
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost : IMessageReceiveHandler<ConnectAcknowledgement>,
        IMessageReceiveHandler<ServerInfo>,
        IMessageReceiveHandler<Print>,
        IMessageReceiveHandler<NetworkStringListBinaryMetaData>,
        IMessageReceiveHandler<NetworkStringListFullUpdate>,
        IMessageReceiveHandler<NetworkStringListUpdate>
    {
        private void RegisterMessageHandlers(MessagesReceiveHandler receiveHandler)
        {
            receiveHandler.RegisterHandler<ConnectAcknowledgement>(this);
            receiveHandler.RegisterHandler<ServerInfo>(this);
            receiveHandler.RegisterHandler<Print>(this);
            receiveHandler.RegisterHandler<NetworkStringListBinaryMetaData>(this);
            receiveHandler.RegisterHandler<NetworkStringListFullUpdate>(this);
            receiveHandler.RegisterHandler<NetworkStringListUpdate>(this);
        }

        public void ReceiveMessage(NetConnection connection, ConnectAcknowledgement message)
        {
            EventSystem.DispatchEvent(EngineEvents.ClientReceivedAck);

            if (ConnectionSetupStatus == ClientConnectionSetupStatus.Connected)
            {
                _logger.Debug("Duplicate connect ack. received.  Ignored.");
                return;
            }

            ConnectionSetupStatus = ClientConnectionSetupStatus.Connected;

            _userId = message.UserId;
            _netClient.Server.TrueAddress = NetUtilities.StringToIPAddress(message.TrueAddress, NetConstants.DefaultServerPort);
            _buildNumber = message.BuildNumber;

            if (_netClient.Server.Connection.RemoteEndPoint.Address != IPAddress.Loopback)
            {
                _logger.Information($"Connection accepted by {_netClient.Server.Name}");
            }
            else
            {
                _logger.Debug("Connection accepted.");
            }

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

            CreateNetworkStringLists();

            _modelPrecache.OnStringAdded += _modelPrecache_OnStringAdded;

            RequestResources();
        }

        private void _modelPrecache_OnStringAdded(IReadOnlyNetworkStringList list, int index)
        {
            var data = list.GetBinaryData(index) as ModelPrecacheData;

            //TODO: process model data
        }

        private void RequestResources()
        {
            _netClient.Server.AddMessage(new SendResources());
        }

        public void ReceiveMessage(NetConnection connection, Print message)
        {
            _logger.Information(message.MessageContents);
        }

        public void ReceiveMessage(NetConnection connection, NetworkStringListBinaryMetaData message)
        {
            _netClient.ReceiveMessage(connection, message);

            RequestResources();
        }

        public void ReceiveMessage(NetConnection connection, NetworkStringListFullUpdate message)
        {
            try
            {
                _netClient.ReceiveMessage(connection, message);
            }
            catch (InvalidOperationException e)
            {
                _logger.Error(e, "An error occurred while processing a string list full update");
                Disconnect(true);
                return;
            }

            RequestResources();
        }

        public void ReceiveMessage(NetConnection connection, NetworkStringListUpdate message)
        {
            _netClient.ReceiveMessage(connection, message);
        }
    }
}
