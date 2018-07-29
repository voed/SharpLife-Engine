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
using SharpLife.Engine.Shared.GameUtils;
using SharpLife.Networking.Shared;
using SharpLife.Utility.Events;
using System;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost : IEngineServerHost
    {
        public ICommandContext CommandContext { get; }

        public IEventSystem EventSystem => _engine.EventSystem;

        public bool GameAssemblyLoaded => _game != null;

        public bool Active { get; private set; }

        private readonly IEngine _engine;

        private readonly ILogger _logger;

        private GameData<IGameServer> _game;

        private readonly IVariable _ipname;
        private readonly IVariable _hostport;
        private readonly IVariable _defport;
        private readonly IVariable _sv_timeout;

        private readonly IVariable _maxPlayers;

        private uint _mapCRC = 0;

        private int _spawnCount = 0;

        public EngineServerHost(IEngine engine, ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CommandContext = _engine.CommandSystem.CreateContext("ServerContext");

            _ipname = CommandContext.RegisterVariable(new VariableInfo("ip")
                .WithHelpInfo("The IP address to use for server hosts")
                .WithValue(NetConstants.LocalHost));

            _hostport = CommandContext.RegisterVariable(new VariableInfo("hostport")
                .WithHelpInfo("The port to use for server hosts")
                .WithValue(0));

            _defport = CommandContext.RegisterVariable(new VariableInfo("port")
               .WithHelpInfo("The default port to use for server hosts")
               .WithValue(NetConstants.DefaultServerPort));

            _sv_timeout = CommandContext.RegisterVariable(new VariableInfo("sv_timeout")
                .WithHelpInfo("Maximum time to wait before timing out client connections")
                .WithValue(60));

            _maxPlayers = CommandContext.RegisterVariable(new VariableInfo("maxplayers")
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

            LoadGameAssembly();

            CreateMessageHandlers();
            RegisterMessageHandlers();
        }

        private void LoadGameAssembly()
        {
            _game = GameLoadUtils.LoadGame<IGameServer>(
                _engine.GameDirectory,
                _engine.GameConfiguration.GameServer.AssemblyName,
                _engine.GameConfiguration.GameServer.EntrypointClass);

            var collection = new ServiceCollection();

            var serviceProvider = collection.BuildServiceProvider();

            _game.Entrypoint.Initialize(serviceProvider);
        }

        public void Shutdown()
        {
            Stop();

            _game?.Entrypoint.Shutdown();

            _engine.CommandSystem.DestroyContext(CommandContext);
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

            if (_game != null)
            {
                if (Active)
                {
                    //TODO: deactivate game
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
