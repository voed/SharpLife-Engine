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
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Engine.API.Game;
using SharpLife.Engine.Server.Clients;
using SharpLife.Engine.Server.Networking;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Events;
using SharpLife.Engine.Shared.ModUtils;
using SharpLife.Networking.Shared;
using SharpLife.Utility.Events;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Host
{
    public class EngineServerHost : IEngineServerHost
    {
        public IConCommandSystem CommandSystem => _engine.CommandSystem;

        public IEventSystem EventSystem => _engine.EventSystem;

        public bool GameAssemblyLoaded => _mod != null;

        public bool Active { get; private set; }

        private readonly IEngine _engine;

        private ModData<IServerMod> _mod;

        private NetworkServer _netServer;

        private readonly IConVar _ipname;
        private readonly IConVar _hostport;
        private readonly IConVar _defport;
        private readonly IConVar _sv_timeout;

        private readonly IConVar _maxPlayers;

        private long _mapCRC = 0;

        private readonly ServerClientList _clientList;

        public EngineServerHost(IEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            _ipname = CommandSystem.RegisterConVar(new ConVarInfo("ip")
                .WithHelpInfo("The IP address to use for server hosts")
                .WithValue(NetConstants.LocalHost));

            _hostport = CommandSystem.RegisterConVar(new ConVarInfo("hostport")
                .WithHelpInfo("The port to use for server hosts")
                .WithValue(0));

            _defport = CommandSystem.RegisterConVar(new ConVarInfo("port")
               .WithHelpInfo("The default port to use for server hosts")
               .WithValue(NetConstants.DefaultServerPort));

            _sv_timeout = CommandSystem.RegisterConVar(new ConVarInfo("sv_timeout")
                .WithHelpInfo("Maximum time to wait before timing out client connections")
                .WithValue(60));

            _maxPlayers = CommandSystem.RegisterConVar(new ConVarInfo("maxplayers")
                .WithHelpInfo("The maximum number of players that can connect to this server")
                .WithValue(_engine.IsDedicatedServer ? 6 : NetConstants.MinClients)
                .WithDelegateFilter((ref string _, ref float __) =>
                {
                    if (Active)
                    {
                        Log.Logger.Information("maxplayers cannot be changed while a server is running.");
                    }

                    return !Active;
                })
                .WithNumberFilter()
                .WithMinMaxFilter(NetConstants.MinClients, NetConstants.MaxClients));

            if (_engine.CommandLine.TryGetValue("-port", out var portValue))
            {
                _hostport.String = portValue;
            }

            if (_engine.CommandLine.TryGetValue("-maxplayers", out var maxPlayersValue))
            {
                _maxPlayers.String = maxPlayersValue;
            }

            _clientList = new ServerClientList(NetConstants.MaxClients, _maxPlayers);
        }

        public void Shutdown()
        {
            Stop();

            _mod?.Entrypoint.Shutdown();
        }

        public void LoadGameAssembly()
        {
            if (_mod == null)
            {
                //Load the game mod assembly
                _mod = ModLoadUtils.LoadMod<IServerMod>(
                    _engine.GameDirectory,
                    _engine.GameConfiguration.ServerMod.AssemblyName,
                    _engine.GameConfiguration.ServerMod.EntrypointClass);

                var collection = new ServiceCollection();

                var serviceProvider = collection.BuildServiceProvider();

                _mod.Entrypoint.Initialize(serviceProvider);
            }
        }

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

        public bool Start(string mapName, string startSpot = null, ServerStartFlags flags = ServerStartFlags.None)
        {
            EventSystem.DispatchEvent(new MapStartedLoading(
                mapName,
                _engine.MapManager.MapName,
                (flags & ServerStartFlags.ChangeLevel) != 0,
                (flags & ServerStartFlags.LoadGame) != 0));

            //TODO: start transitioning clients

            CreateNetworkServer();

            Log.Logger.Information($"Loading map \"{mapName}\"");

            //TODO: print server vars

            //TODO: set hostname

            if (startSpot != null)
            {
                Log.Logger.Debug($"Spawn Server {mapName}: [{startSpot}]\n");
            }
            else
            {
                Log.Logger.Debug($"Spawn Server {mapName}\n");
            }

            //TODO: clear custom data if size exceeds maximum

            //TODO: allocate client memory

            EventSystem.DispatchEvent(EngineEvents.ServerMapDataStartLoad);

            if (!_engine.MapManager.LoadMap(mapName))
            {
                Log.Logger.Information($"Couldn't spawn server {_engine.MapManager.FormatMapFileName(mapName)}\n");
                Stop();
                return false;
            }

            EventSystem.DispatchEvent(EngineEvents.ServerMapDataFinishLoad);

            if (!_engine.MapManager.ComputeCRC(mapName, out var crc))
            {
                Stop();
                return false;
            }

            _mapCRC = crc;

            EventSystem.DispatchEvent(EngineEvents.ServerMapCRCComputed);

            //TODO: add world to precache, precache bsp models

            return true;
        }

        public void Activate()
        {
            //TODO: implement
            Active = true;
        }

        public void Deactivate()
        {
            //TODO: implement
            //TODO: notify Steam

            if (_mod != null)
            {
                if (Active)
                {
                    //TODO: deactivate mod
                }
            }
        }

        public void Stop()
        {
            //TODO: implement
            if (Active)
            {
                Deactivate();

                Active = false;

                foreach (var client in _clientList)
                {
                    DropClient(client, NetMessages.ServerShutdownMessage);
                }
            }

            //Always shut down the networking system, even if we weren't active
            _netServer?.Shutdown(NetMessages.ServerShutdownMessage);
        }

        public void RunFrame(float deltaSeconds)
        {
            if (!Active)
            {
                return;
            }

            _netServer.ReadPackets(HandlePacket);
        }

        private ServerClient FindClient(IPEndPoint endPoint)
        {
            var client = _clientList.FindClientByEndPoint(endPoint);

            if (client != null)
            {
                return client;
            }

            Log.Logger.Warning($"Client with IP {endPoint} has no associated client instance");

            return null;
        }

        private void DropClient(ServerClient client, string reason)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (reason == null)
            {
                throw new ArgumentNullException(nameof(reason));
            }

            //TODO: notify game

            Log.Logger.Information($"Dropped {client.Name} from server\nReason:  {reason}");

            client.Disconnect(reason);

            //To ensure that clients get the disconnect order
            _netServer.FlushOutgoingPackets();
        }

        private void HandlePacket(NetIncomingMessage message)
        {
            //TODO: filter packets

            switch (message.MessageType)
            {
                case NetIncomingMessageType.Error:
                    Log.Logger.Error("An unknown error occurred");
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
                    Log.Logger.Verbose(message.ReadString());
                    break;

                case NetIncomingMessageType.DebugMessage:
                    Log.Logger.Debug(message.ReadString());
                    break;

                case NetIncomingMessageType.WarningMessage:
                    Log.Logger.Warning(message.ReadString());
                    break;

                case NetIncomingMessageType.ErrorMessage:
                    Log.Logger.Error(message.ReadString());
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

            //TODO: validate input data

            message.SenderConnection.Approve();

            //TODO: get user name
            var client = ServerClient.CreateClient(message.SenderConnection, slot, "unnamed");

            _clientList.AddClientToSlot(client);
        }

        private void HandleStatusChanged(NetIncomingMessage message)
        {
            var status = (NetConnectionStatus)message.ReadByte();

            string reason = message.ReadString();

            var client = FindClient(message.SenderEndPoint);

            if (client != null)
            {
                switch (status)
                {
                    case NetConnectionStatus.Connected:
                        client.Connected = true;
                        break;

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
            //TODO: implement
        }
    }
}
