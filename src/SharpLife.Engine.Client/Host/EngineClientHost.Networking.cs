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

using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.Client.Networking;
using SharpLife.Engine.Shared.Events;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication;
using SharpLife.Networking.Shared.Communication.MessageMapping;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;
using System;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost
    {
        private NetworkClient _netClient;

        private IReadOnlyNetworkStringList _modelPrecache;

        /// <summary>
        /// Creates the network client, using current client configuration values
        /// </summary>
        private void SetupNetworking()
        {
            if (_netClient == null)
            {
                var receiveHandler = new MessagesReceiveHandler(_logger, NetMessages.ServerToClientMessages, true);

                RegisterMessageHandlers(receiveHandler);

                _netClient = new NetworkClient(
                    _logger,
                    this,
                    new SendMappings(NetMessages.ClientToServerMessages),
                    receiveHandler,
                    _cl_name,
                    NetConstants.AppIdentifier,
                    _clientport.Integer,
                    _cl_resend.Float,
                    _cl_timeout.Float);

                _netClient.Start();
            }
        }

        private void CreateNetworkStringLists()
        {
            var receiver = _netClient.StringListReceiver;

            receiver.RegisterBinaryType(ModelPrecacheData.Descriptor);

            _modelPrecache = receiver.CreateList("ModelPrecache");

            //TODO: let game do the same
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

            //TODO: initialize client state

            if (_netClient.IsConnected && !_netClient.IsDisconnecting)
            {
                Disconnect(false);
            }

            _netClient.Connect(address);
        }

        private void Disconnect(ICommandArgs command)
        {
            Disconnect(true);
        }

        public void Disconnect(bool shutdownServer)
        {
            //Always dispatch, even if we're not connected
            EventSystem.DispatchEvent(EngineEvents.ClientStartDisconnect);

            _netClient?.Disconnect(NetMessages.ClientDisconnectMessage);

            ConnectionSetupStatus = ClientConnectionSetupStatus.NotConnected;

            EventSystem.DispatchEvent(EngineEvents.ClientEndDisconnect);

            if (shutdownServer)
            {
                _engine.StopServer();
            }

            _renderer.ClearBSP();

            //TODO: reset client state
        }
    }
}
