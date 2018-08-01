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
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.Server;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost :
        IMessageReceiveHandler<NewConnection>,
        IMessageReceiveHandler<SendResources>
    {
        private void RegisterMessageHandlers(MessagesReceiveHandler receiveHandler)
        {
            receiveHandler.RegisterHandler<NewConnection>(this);
            receiveHandler.RegisterHandler<SendResources>(this);
        }

        public void ReceiveMessage(NetConnection connection, NewConnection message)
        {
            var client = ClientList.FindClientByEndPoint(connection.RemoteEndPoint);

            if (!client.Spawned || client.Active)
            {
                client.SetupStage = ServerClientSetupStage.AwaitingResourceTransmissionStart;

                client.ConnectionStarted = _engine.EngineTime.ElapsedTime;

                //TODO: send custom user messages

                SendServerInfo(client);
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

        /// <summary>
        /// Client requests for resources during setup will send data in sequence
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        public void ReceiveMessage(NetConnection connection, SendResources message)
        {
            var client = ClientList.FindClientByEndPoint(connection.RemoteEndPoint);

            switch (client.SetupStage)
            {
                case ServerClientSetupStage.AwaitingResourceTransmissionStart:
                    {
                        client.SetupStage = ServerClientSetupStage.SendingStringListsBinaryMetaData;

                        client.AddMessages(_netServer.StringListTransmitter.CreateBinaryTypesMessages(), true);
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
                        client.NextStringListToSend = client.LastStringListFullUpdate + 1;
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
