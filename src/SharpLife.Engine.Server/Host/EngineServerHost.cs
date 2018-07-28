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

using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Engine.API.Game.Server;
using SharpLife.Engine.Server.Clients;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Events;
using SharpLife.Engine.Shared.ModUtils;
using SharpLife.Networking.Shared;
using SharpLife.Utility.Events;
using System;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost : IEngineServerHost
    {
        public IConCommandSystem CommandSystem => _engine.CommandSystem;

        public IEventSystem EventSystem => _engine.EventSystem;

        public bool GameAssemblyLoaded => _mod != null;

        public bool Active { get; private set; }

        private readonly IEngine _engine;

        private readonly ILogger _logger;

        private ModData<IServerMod> _mod;

        private readonly IConVar _ipname;
        private readonly IConVar _hostport;
        private readonly IConVar _defport;
        private readonly IConVar _sv_timeout;

        private readonly IConVar _maxPlayers;

        private uint _mapCRC = 0;

        private int _spawnCount = 0;

        public EngineServerHost(IEngine engine, ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                        _logger.Information("maxplayers cannot be changed while a server is running.");
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

            CreateMessageHandlers();
            RegisterMessageHandlers();
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

        public bool Start(string mapName, string startSpot = null, ServerStartFlags flags = ServerStartFlags.None)
        {
            EventSystem.DispatchEvent(new MapStartedLoading(
                mapName,
                _engine.MapManager.MapName,
                (flags & ServerStartFlags.ChangeLevel) != 0,
                (flags & ServerStartFlags.LoadGame) != 0));

            //TODO: start transitioning clients

            CreateNetworkServer();

            _logger.Information($"Loading map \"{mapName}\"");

            //TODO: print server vars

            //TODO: set hostname

            if (startSpot != null)
            {
                _logger.Debug($"Spawn Server {mapName}: [{startSpot}]\n");
            }
            else
            {
                _logger.Debug($"Spawn Server {mapName}\n");
            }

            ++_spawnCount;

            //TODO: clear custom data if size exceeds maximum

            //TODO: allocate client memory

            EventSystem.DispatchEvent(EngineEvents.ServerMapDataStartLoad);

            var mapFileName = _engine.MapManager.FormatMapFileName(mapName);

            if (!_engine.MapManager.LoadMap(mapFileName))
            {
                _logger.Information($"Couldn't spawn server {mapFileName}\n");
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

            foreach (var client in _clientList)
            {
                _netServer.SendClientMessages(client);
            }
        }
    }
}
