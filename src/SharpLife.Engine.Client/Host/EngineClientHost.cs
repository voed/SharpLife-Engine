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
using SDL2;
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Engine.Client.Resources;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.API.Engine.Client;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Engine.Shared.API.Game.Client;
using SharpLife.Engine.Shared.API.Game.Shared;
using SharpLife.Engine.Shared.CommandSystem;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Logging;
using SharpLife.Engine.Shared.UI;
using SharpLife.FileSystem;
using SharpLife.Game.Client.API;
using SharpLife.Models;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Utility;
using SharpLife.Utility.Events;
using SharpLife.Utility.FileSystem;
using System;
using System.IO;
using System.Net;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost : IEngineClientHost, IClientEngine
    {
        public IFileSystem FileSystem => _engine.FileSystem;

        public ICommandContext CommandContext { get; }

        public IEventSystem EventSystem => _engine.EventSystem;

        public IBridge GameBridge { get; private set; }

        public ILogListener LogListener
        {
            get => _engine.LogTextWriter.Listener;
            set => _engine.LogTextWriter.Listener = value;
        }

        public IWindow GameWindow { get; }

        public IUserInterface UserInterface { get; }

        public ITime Time => _engine.EngineTime;

        public IModelManager ModelManager => _engine.ModelManager;

        public IEngineModels Models => _clientModels;

        private readonly IEngine _engine;

        private readonly ILogger _logger;

        //Engine API
        private readonly ClientModels _clientModels;

        //Game API
        private IGameClient _game;

        private readonly IVariable _clientport;
        private readonly IVariable _cl_resend;
        private readonly IVariable _cl_timeout;

        private readonly IVariable _net_cl_log_messages;

        private readonly IVariable _cl_name;

        public EngineClientHost(IEngine engine, ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CommandContext = _engine.CommandSystem.CreateContext("ClientContext");

            UserInterface = _engine.CreateUserInterface();

            var gameWindowName = _engine.EngineConfiguration.DefaultGameName;

            if (!string.IsNullOrWhiteSpace(_engine.EngineConfiguration.GameName))
            {
                gameWindowName = _engine.EngineConfiguration.GameName;
            }

            GameWindow = UserInterface.CreateMainWindow(gameWindowName, _engine.CommandLine.Contains("-noborder") ? SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS : 0);

            GameWindow.Center();

            CommandContext.RegisterCommand(new CommandInfo("connect", Connect).WithHelpInfo("Connect to a server"));
            CommandContext.RegisterCommand(new CommandInfo("disconnect", Disconnect).WithHelpInfo("Disconnect from a server"));

            _clientport = CommandContext.RegisterVariable(new VariableInfo("clientport")
                .WithHelpInfo("Client port to use for connections")
                .WithValue(NetConstants.DefaultClientPort)
                .WithMinMaxFilter(IPEndPoint.MinPort, IPEndPoint.MaxPort, true));

            _cl_resend = CommandContext.RegisterVariable(new VariableInfo("cl_resend")
                .WithHelpInfo("Maximum time to wait before resending a client connection request")
                .WithValue(6.0f)
                .WithMinMaxFilter(1.5f, 20.0f));

            _cl_timeout = CommandContext.RegisterVariable(new VariableInfo("cl_timeout")
                .WithHelpInfo("Maximum time to wait before timing out server connections")
                .WithValue(60)
                .WithEngineFlags(EngineCommandFlags.Archive));

            _net_cl_log_messages = CommandContext.RegisterVariable(new VariableInfo("net_cl_log_messages")
                .WithHelpInfo("Whether to log received server-to-client network messages")
                .WithValue(false)
                .WithBooleanFilter()
                .WithChangeHandler((ref VariableChangeEvent @event) =>
                {
                    if (_netClient != null)
                    {
                        _netClient.TraceMessageLogging = @event.Boolean;
                    }
                }));

            //TODO: add change handler to send update to server if connected
            _cl_name = CommandContext.RegisterVariable(new VariableInfo("name")
                .WithHelpInfo("Your name as seen by other players"));

            _clientModels = new ClientModels(_engine.ModelManager);

            LoadGameClient();

            var objectListTypeRegistryBuilder = new TypeRegistryBuilder();

            _clientNetworking.RegisterObjectListTypes(objectListTypeRegistryBuilder);

            _objectListTypeRegistry = objectListTypeRegistryBuilder.BuildRegistry();

            var dataSetBuilder = new BinaryDataSetBuilder();

            RegisterNetworkBinaryData(dataSetBuilder);

            _binaryDataDescriptorSet = dataSetBuilder.BuildReceptionSet();
        }

        private void LoadGameClient()
        {
            _game = new GameClient();

            //Set up services
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_logger);
            serviceCollection.AddSingleton<IClientEngine>(this);
            serviceCollection.AddSingleton(_engine.EngineTime);
            serviceCollection.AddSingleton<IEngineModels>(_clientModels);

            _game.Initialize(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            GameBridge = serviceProvider.GetRequiredService<IBridge>();
            _clientNetworking = serviceProvider.GetRequiredService<IClientNetworking>();

            _game.Startup(serviceProvider);

            _engine.ModelManager.SetLoaders(_game.GetModelLoaders());
        }

        public void Shutdown()
        {
            WriteConfigFile();

            Disconnect(false);

            _game.Shutdown();

            if (_netClient != null)
            {
                _netClient.Shutdown(NetMessages.ClientShutdownMessage);
                _netClient = null;
            }

            _engine.CommandSystem.DestroyContext(CommandContext);
        }

        private void WriteConfigFile()
        {
            var configFileName = $"cfg/config{FileExtensionUtils.AsExtension(Framework.Extension.CFG)}";

            try
            {
                using (var writer = _engine.FileSystem.CreateText(configFileName, FileSystemConstants.PathID.GameConfig))
                {
                    writer.WriteLine("// This file is overwritten whenever you change your user settings in the game.");
                    writer.WriteLine("// Add custom configurations to the file \"userconfig.cfg\".");
                    writer.WriteLine();

                    writer.WriteLine("unbindall");

                    WriteCommandVariables(writer);

                    //TODO: save all config vars
                }
            }
            catch (FileNotFoundException)
            {
                _logger.Information($"Couldn't write {configFileName}.");
            }
        }

        private void WriteCommandVariables(TextWriter writer)
        {
            foreach (var command in CommandContext.Commands.Values)
            {
                if (command is IVariable var
                    && (var.EngineFlags() & EngineCommandFlags.Archive) != 0)
                {
                    writer.WriteLine($"{var.Name} \"{var.String}\"");
                }
            }
        }

        public void Update(float deltaSeconds)
        {
            if (_netClient != null)
            {
                //Always read packets, even if not connected to process disconnects fully
                _netClient.ReadPackets();
                _netClient.RunFrame();
            }

            _game.Update(deltaSeconds);

            //Only send messages if we're still actively connected, once we start disconnecting all user messages should be stopped
            if (_netClient != null && _netClient.IsConnected && !_netClient.IsDisconnecting)
            {
                _netClient.SendServerMessages(_netClient.Server);
            }
        }

        public void Draw()
        {
            _game.Draw();
        }

        public void EndGame(string reason)
        {
            _engine.EndGame(reason);
        }
    }
}
