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

using SharpLife.Engine.Server.Networking;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication;
using SharpLife.Networking.Shared.Communication.MessageMapping;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost
    {
        private NetworkServer _netServer;

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
                    new SendMappings(NetMessages.ServerToClientMessages),
                    receiveHandler,
                    NetConstants.AppIdentifier,
                    ipAddress,
                    NetConstants.MaxClients,
                    _sv_timeout.Float
                    );

                _netServer.Start();
            }
        }

        private void CreateNetworkStringLists()
        {
            var transmitter = _netServer.StringListTransmitter;

            transmitter.BinaryDataDescriptorSet.Add(ModelPrecacheData.Descriptor);

            _modelPrecache = transmitter.CreateList("ModelPrecache");

            //TODO: let game do the same
        }
    }
}
