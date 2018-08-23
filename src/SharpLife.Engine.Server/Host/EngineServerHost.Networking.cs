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

using SharpLife.Engine.API.Engine.Server;
using SharpLife.Engine.API.Game.Server;
using SharpLife.Engine.Server.Networking;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Communication.Messages;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost : IServerNetworkListener
    {
        private NetworkServer _netServer;

        private BinaryDataTransmissionDescriptorSet _binaryDataDescriptorSet;

        private readonly TypeRegistry _objectListTypeRegistry;

        private IServerNetworking _serverNetworking;

        private INetworkStringList _modelPrecache;

        private void CreateNetworkServer()
        {
            if (_netServer == null)
            {
                var port = _hostport.Integer;

                if (port == 0)
                {
                    port = _defport.Integer;

                    _hostport.Integer = _defport.Integer;
                }

                var ipAddress = NetUtilities.StringToIPAddress(_ipname.String, port);

                var receiveHandler = new MessagesReceiveHandler(_logger, NetMessages.ClientToServerMessages, true);

                RegisterMessageHandlers(receiveHandler);

                //Always allow the maximum number of clients since we can't just recreate the server whenever we want (clients stay connected through map changes)
                _netServer = new NetworkServer(
                    _logger,
                    this,
                    this,
                    new SendMappings(NetMessages.ServerToClientMessages),
                    receiveHandler,
                    _binaryDataDescriptorSet,
                    _objectListTypeRegistry,
                    _engine.EngineTime,
                    _maxPlayers,
                    NetConstants.AppIdentifier,
                    ipAddress,
                    NetConstants.MaxClients,
                    _sv_timeout.Float
                    );

                _netServer.Start();
            }

            _netServer.OnNewMapStarted();
        }

        private void RegisterNetworkBinaryData(IBinaryDataSetBuilder dataSetBuilder)
        {
            NetMessages.RegisterEngineBinaryDataTypes(dataSetBuilder);

            //TODO: let game do the same
        }

        public void CreateNetworkStringLists(INetworkStringListsBuilder networkStringListBuilder)
        {
            _modelPrecache = networkStringListBuilder.CreateList("ModelPrecache");

            //TODO: let game do the same
        }

        public void CreateNetworkObjectLists(IServerNetworkObjectLists networkObjectLists)
        {
            _serverNetworking.CreateNetworkObjectLists(networkObjectLists);
        }
    }
}
