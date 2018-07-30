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
using SharpLife.Engine.API.Engine.Client;
using SharpLife.Engine.API.Game.Client;
using SharpLife.Engine.Client.Networking;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.CommandSystem;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.GameUtils;
using SharpLife.Engine.Shared.Logging;
using SharpLife.Engine.Shared.UI;
using SharpLife.FileSystem;
using SharpLife.Networking.Shared;
using SharpLife.Utility;
using SharpLife.Utility.Events;
using System;
using System.IO;
using System.Net;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost : IEngineClientHost, IClientEngine
    {
        public ICommandContext CommandContext { get; }

        public IEventSystem EventSystem => _engine.EventSystem;

        public ClientConnectionStatus ConnectionStatus { get; set; }

        public ILogListener LogListener
        {
            get => _engine.LogTextWriter.Listener;
            set => _engine.LogTextWriter.Listener = value;
        }

        private readonly IEngine _engine;

        private readonly ILogger _logger;

        private readonly IUserInterface _userInterface;

        private readonly IWindow _window;

        private readonly Renderer.Renderer _renderer;

        private GameData<IGameClient> _game;

        private IClientUI _clientUI;

        private readonly IVariable _clientport;
        private readonly IVariable _cl_resend;
        private readonly IVariable _cl_timeout;

        private readonly IVariable _cl_name;

        private int _userId;
        private int _buildNumber;

        public EngineClientHost(IEngine engine, ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CommandContext = _engine.CommandSystem.CreateContext("ClientContext");

            _userInterface = _engine.CreateUserInterface();

            var gameWindowName = _engine.EngineConfiguration.DefaultGameName;

            if (!string.IsNullOrWhiteSpace(_engine.GameConfiguration.GameName))
            {
                gameWindowName = _engine.GameConfiguration.GameName;
            }

            _window = _userInterface.CreateMainWindow(gameWindowName, _engine.CommandLine.Contains("-noborder") ? SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS : 0);

            _window.Center();

            _renderer = new Renderer.Renderer(
                _window.WindowHandle,
                _window.GLContextHandle,
                _engine.FileSystem,
                _userInterface.WindowManager.InputSystem,
                Framework.Path.EnvironmentMaps,
                Framework.Path.Shaders);

            _window.Resized += _renderer.WindowResized;

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

            //TODO: add change handler to send update to server if connected
            _cl_name = CommandContext.RegisterVariable(new VariableInfo("name")
                .WithHelpInfo("Your name as seen by other players"));
        }

        public void PostInitialize()
        {
            _window.Center();

            LoadGameAssembly();
        }

        private void LoadGameAssembly()
        {
            _game = GameLoadUtils.LoadGame<IGameClient>(
                _engine.GameDirectory,
                _engine.GameConfiguration.GameClient.AssemblyName,
                _engine.GameConfiguration.GameClient.EntrypointClass);

            //Set up services
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_logger);
            serviceCollection.AddSingleton<IClientEngine>(this);
            serviceCollection.AddSingleton<IViewState>(_renderer);

            _game.Entrypoint.Initialize(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _clientUI = serviceProvider.GetRequiredService<IClientUI>();

            _game.Entrypoint.Startup(serviceProvider);
        }

        public void Shutdown()
        {
            WriteConfigFile();

            Disconnect(false);

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
            if (ConnectionStatus != ClientConnectionStatus.NotConnected)
            {
                _netClient.ReadPackets(HandlePacket);
            }

            _renderer.Update(deltaSeconds);

            _clientUI.Update(deltaSeconds);

            if (ConnectionStatus != ClientConnectionStatus.NotConnected)
            {
                _netClient.SendServerMessages(_netClient.Server);
            }
        }

        public void Draw()
        {
            _clientUI.Draw();

            _renderer.Draw();
        }
    }
}
