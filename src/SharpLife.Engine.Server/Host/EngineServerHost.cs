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
using SharpLife.Engine.Server.Resources;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Engine.Shared.API.Game.Server;
using SharpLife.Engine.Shared.API.Game.Shared;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Events;
using SharpLife.FileSystem;
using SharpLife.Game.Server.API;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Utility;
using SharpLife.Utility.Events;
using System;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost : IEngineServerHost, IServerEngine
    {
        public IFileSystem FileSystem => _engine.FileSystem;

        public ICommandContext CommandContext { get; }

        public ITime EngineTime => _engine.EngineTime;

        public IEventSystem EventSystem => _engine.EventSystem;

        //TODO: make sure the client list is always valid for game code
        public IServerClients Clients => _netServer.ClientList;

        public bool IsDedicatedServer => _engine.IsDedicatedServer;

        public bool Active { get; private set; }

        private readonly IEngine _engine;

        private readonly ILogger _logger;

        //Engine API
        private readonly ServerModels _serverModels;

        //Game API
        private IGameServer _game;

        private readonly IVariable _ipname;
        private readonly IVariable _hostport;
        private readonly IVariable _defport;
        private readonly IVariable _sv_timeout;

        private readonly IVariable _net_sv_log_messages;

        private readonly IVariable _maxPlayers;

        private int _spawnCount = 0;

        public EngineServerHost(IEngine engine, ILogger logger, IBridge gameBridge)
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

            _net_sv_log_messages = CommandContext.RegisterVariable(new VariableInfo("net_sv_log_messages")
                .WithHelpInfo("Whether to log received client-to-server network messages")
                .WithValue(false)
                .WithBooleanFilter()
                .WithChangeHandler((ref VariableChangeEvent @event) =>
                {
                    if (_netServer != null)
                    {
                        _netServer.TraceMessageLogging = @event.Boolean;
                    }
                }));

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

            _serverModels = new ServerModels(_engine.ModelManager, Framework.FallbackModelName);

            LoadGameServer(gameBridge);

            var objectListTypeRegistryBuilder = new TypeRegistryBuilder();

            _serverNetworking.RegisterObjectListTypes(objectListTypeRegistryBuilder);

            _objectListTypeRegistry = objectListTypeRegistryBuilder.BuildRegistry();

            var dataSetBuilder = new BinaryDataSetBuilder();

            RegisterNetworkBinaryData(dataSetBuilder);

            _binaryDataDescriptorSet = dataSetBuilder.BuildTransmissionSet();
        }

        private void LoadGameServer(IBridge gameBridge)
        {
            _game = new GameServer();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_logger);
            serviceCollection.AddSingleton<IServerEngine>(this);

            if (gameBridge != null)
            {
                serviceCollection.AddSingleton(gameBridge);
            }

            serviceCollection.AddSingleton(_engine.EngineTime);
            serviceCollection.AddSingleton<IEngineModels>(_serverModels);
            serviceCollection.AddSingleton<IServerModels>(_serverModels);

            _game.Initialize(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _serverNetworking = serviceProvider.GetRequiredService<IServerNetworking>();

            _game.Startup(serviceProvider);

            //Listen servers have the client do this
            if (_engine.IsDedicatedServer)
            {
                _engine.ModelManager.SetLoaders(_game.GetModelLoaders());
            }
        }

        public void Shutdown()
        {
            Stop();

            _game.Shutdown();

            //Always shut down the networking system, even if we weren't active
            _netServer?.Shutdown(NetMessages.ServerShutdownMessage);

            _engine.CommandSystem.DestroyContext(CommandContext);
        }

        public bool Start(string mapName, string startSpot = null, ServerStartFlags flags = ServerStartFlags.None)
        {
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

            if (!_game.TryMapLoadBegin(mapName, flags))
            {
                Stop();
                return false;
            }

            return true;
        }

        public void InitializeMap(ServerStartFlags flags)
        {
            _game.MapLoadContinue((flags & ServerStartFlags.LoadGame) != 0);

            //Engine can handle map load stuff here if needed

            _game.MapLoadFinished();
        }

        public void Activate()
        {
            _game.Activate();

            //TODO: implement
            Active = true;

            _game.PostActivate();
        }

        public void Deactivate()
        {
            //TODO: implement
            //TODO: notify Steam

            if (_game != null)
            {
                if (Active)
                {
                    _game.Deactivate();
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

                foreach (var client in _netServer.ClientList)
                {
                    _netServer.DropClient(client, NetMessages.ServerShutdownMessage);
                }
            }
        }

        public void RunFrame(float deltaSeconds)
        {
            //Always process packets so we can handle disconnection properly after listen server shutdown
            _netServer?.ReadPackets();

            if (!Active)
            {
                return;
            }

            _game.RunFrame();

            _netServer.RunFrame();
        }

        public bool IsMapValid(string mapName)
        {
            return _game.IsMapValid(mapName);
        }
    }
}
