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
using SharpLife.Engine.Server.Clients;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication;
using SharpLife.Networking.Shared.MessageMapping;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.Server;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost : IMessageReceiveHandler<NewConnection>
    {
        private SendMappings _netSendMappings;
        private MessagesReceiveHandler _netReceiveHandler;

        private void CreateMessageHandlers()
        {
            _netSendMappings = new SendMappings(NetMessages.ServerToClientMessages);
            _netReceiveHandler = new MessagesReceiveHandler(_logger, NetMessages.ClientToServerMessages, true);
        }

        private void RegisterMessageHandlers()
        {
            _netReceiveHandler.RegisterHandler<NewConnection>(this);
        }

        public void ReceiveMessage(NetConnection connection, NewConnection message)
        {
            var client = _clientList.FindClientByEndPoint(connection.RemoteEndPoint);

            if (!client.Spawned || client.Active)
            {
                client.ConnectionStarted = _engine.EngineTime.ElapsedTime;

                //TODO: send custom user messages

                SendServerInfo(client);

                client.Connected = true;
            }
        }

        private void SendServerInfo(ServerClient client)
        {
            //TODO: add developer cvar
            //TODO: define build number
            const int buildNumber = 0;
            client.AddMessage(new Print { MessageContents = $"{(char)2}\nBUILD {buildNumber} SERVER (0 CRC)\nServer # {_spawnCount}" }, true);

            client.AddMessage(new ServerInfo
            {
                ProtocolVersion = NetConstants.ProtocolVersion,
                SpawnCount = _spawnCount,
                MapCrc = _mapCRC,
                ClientDllMd5 = ByteString.Empty, //TODO: define
                MaxClients = (uint)_maxPlayers.Integer,
                GameName = _engine.GameDirectory,
                HostName = "", //TODO: define cvar
                //In case the file format/directory ever changes, use the full file name
                MapFileName = _engine.MapManager.FormatMapFileName(_engine.MapManager.MapName),
                AllowCheats = false, //TODO: define cvar
            }, true);

            //TODO: tell game to send its own info now

            //TODO: send game networking data
        }
    }
}
