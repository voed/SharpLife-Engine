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
using SharpLife.Engine.Server.Clients;
using SharpLife.Engine.Server.Networking;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.Server;
using System.Net;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost
    {
        private NetworkServer _netServer;

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

                //Always allow the maximum number of clients since we can't just recreate the server whenever we want (clients stay connected through map changes)
                _netServer = new NetworkServer(
                    NetConstants.AppIdentifier,
                    ipAddress,
                    NetConstants.MaxClients,
                    _sv_timeout.Float
                    );

                _netServer.Start();
            }
        }

        private void HandlePacket(NetIncomingMessage message)
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

        private void HandleConnectionApproval(NetIncomingMessage message)
        {
            //TODO: implement
            //Query IP ban list, other things

            //Check if there is a slot to put the client in
            var slot = _clientList.FindEmptySlot();

            if (slot == -1)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedNoFreeSlots);
                return;
            }

            var protocolVersion = message.ReadVariableUInt32();

            if (protocolVersion < NetConstants.ProtocolVersion)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedProtocolVersionOlder);
                return;
            }
            else if (protocolVersion > NetConstants.ProtocolVersion)
            {
                message.SenderConnection.Deny(NetMessages.ServerClientDeniedProtocolVersionNewer);
                return;
            }

            using (var stream = new NetBufferStream(message))
            {
                var userInfo = ClientUserInfo.Parser.ParseDelimitedFrom(stream);

                //TODO: validate input data

                message.SenderConnection.Approve();

                var name = userInfo.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "unnamed";
                }

                var client = ServerClient.CreateClient(_netSendMappings, message.SenderConnection, slot, _nextUserId++, name);

                _clientList.AddClientToSlot(client);

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

                //Send immediately
                _netServer.SendClientMessages(client);
            }
        }

        private void HandleStatusChanged(NetIncomingMessage message)
        {
            var status = (NetConnectionStatus)message.ReadByte();

            //Since we look up the client below we have to make sure only those states that we care about are handled
            //Some states will occur before the client is given a slot, so don't process those
            var ignore = true;

            switch (status)
            {
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
                    case NetConnectionStatus.Disconnecting:
                        client.Connected = false;
                        break;

                    case NetConnectionStatus.Disconnected:
                        _clientList.RemoveClient(client);
                        break;
                }
            }
        }

        private void HandleData(NetIncomingMessage message)
        {
            _netReceiveHandler.ReadMessages(message.SenderConnection, message);
        }
    }
}
