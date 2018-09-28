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
using SharpLife.Engine.Shared.API.Game.Client;
using SharpLife.Engine.Shared.Events;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Communication.Messages;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using System;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost : IClientNetworkListener
    {
        private NetworkClient _netClient;

        private readonly BinaryDataReceptionDescriptorSet _binaryDataDescriptorSet;

        private readonly TypeRegistry _objectListTypeRegistry;

        private IClientNetworking _clientNetworking;

        /// <summary>
        /// Creates the network client, using current client configuration values
        /// </summary>
        private void SetupNetworking()
        {
            if (_netClient == null)
            {
                var receiveHandler = new MessagesReceiveHandler(_logger, NetMessages.ServerToClientMessages, _net_cl_log_messages.Boolean);

                RegisterMessageHandlers(receiveHandler);

                _netClient = new NetworkClient(
                    _logger,
                    this,
                    this,
                    new SendMappings(NetMessages.ClientToServerMessages),
                    receiveHandler,
                    _binaryDataDescriptorSet,
                    _objectListTypeRegistry,
                    _cl_name,
                    NetConstants.AppIdentifier,
                    _clientport.Integer,
                    _cl_resend.Float,
                    _cl_timeout.Float);

                _netClient.Start();
            }
        }

        private void RegisterNetworkBinaryData(IBinaryDataSetBuilder dataSetBuilder)
        {
            NetMessages.RegisterEngineBinaryDataTypes(dataSetBuilder);

            //TODO: let game do the same
        }

        public void CreateNetworkStringLists(INetworkStringListsBuilder networkStringListBuilder)
        {
            _clientModels.CreateNetworkStringLists(networkStringListBuilder);

            //TODO: let game do the same
        }

        public void CreateNetworkObjectLists(INetworkObjectListReceiverBuilder networkObjectListBuilder)
        {
            _clientNetworking.CreateNetworkObjectLists(networkObjectListBuilder);
        }

        public void OnStringListsReceived()
        {
            //All resources are loaded, let's start
            //TODO: implement

            _game.MapLoadBegin();

            //Engine can handle map load stuff here if needed

            _game.MapLoadFinished();
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

            EventSystem.DispatchEvent(EngineEvents.ClientEndDisconnect);

            if (shutdownServer)
            {
                _engine.StopServer();
            }

            //TODO: refactor into separate method
            _game.MapShutdown();

            //TODO: reset client state
        }
    }
}
