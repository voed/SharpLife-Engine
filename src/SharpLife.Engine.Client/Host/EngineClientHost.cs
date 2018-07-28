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

using ImGuiNET;
using SDL2;
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Engine.API.Game;
using SharpLife.Engine.Client.Networking;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.ModUtils;
using SharpLife.Engine.Shared.UI;
using SharpLife.Networking.Shared;
using SharpLife.Utility;
using SharpLife.Utility.Events;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost : IEngineClientHost
    {
        public IConCommandSystem CommandSystem => _engine.CommandSystem;

        public IEventSystem EventSystem => _engine.EventSystem;

        public ClientConnectionStatus ConnectionStatus { get; set; }

        private readonly IEngine _engine;

        private readonly ILogger _logger;

        private readonly IUserInterface _userInterface;

        private readonly IWindow _window;

        private readonly Renderer.Renderer _renderer;

        private readonly FrameTimeAverager _fta = new FrameTimeAverager(0.666);

        private ModData<IClientMod> _mod;

        private readonly IConVar _clientport;
        private readonly IConVar _cl_resend;
        private readonly IConVar _cl_timeout;

        private readonly IConVar _cl_name;

        private int _userId;
        private int _buildNumber;

        public EngineClientHost(IEngine engine, ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

            CommandSystem.RegisterConCommand(new ConCommandInfo("connect", Connect).WithHelpInfo("Connect to a server"));
            CommandSystem.RegisterConCommand(new ConCommandInfo("disconnect", Disconnect).WithHelpInfo("Disconnect from a server"));

            _clientport = CommandSystem.RegisterConVar(new ConVarInfo("clientport")
                .WithHelpInfo("Client port to use for connections")
                .WithValue(NetConstants.DefaultClientPort)
                .WithMinMaxFilter(IPEndPoint.MinPort, IPEndPoint.MaxPort, true));

            _cl_resend = CommandSystem.RegisterConVar(new ConVarInfo("cl_resend")
                .WithHelpInfo("Maximum time to wait before resending a client connection request")
                .WithValue(6.0f)
                .WithMinMaxFilter(1.5f, 20.0f));

            _cl_timeout = CommandSystem.RegisterConVar(new ConVarInfo("cl_timeout")
                .WithHelpInfo("Maximum time to wait before timing out server connections")
                .WithValue(60)
                .WithFlags(CommandFlags.Archive));

            //TODO: add change handler to send update to server if connected
            _cl_name = CommandSystem.RegisterConVar(new ConVarInfo("name")
                .WithHelpInfo("Your name as seen by other players"));
        }

        public void PostInitialize()
        {
            _window.Center();

            //Load the game mod assembly
            _mod = ModLoadUtils.LoadMod<IClientMod>(
                _engine.GameDirectory,
                _engine.GameConfiguration.ClientMod.AssemblyName,
                _engine.GameConfiguration.ClientMod.EntrypointClass);
        }

        public void Shutdown()
        {
            Disconnect(false);
        }

        public void Update(float deltaSeconds)
        {
            _fta.AddTime(deltaSeconds);

            if (ConnectionStatus != ClientConnectionStatus.NotConnected)
            {
                _netClient.ReadPackets(HandlePacket);
            }

            _renderer.Update(deltaSeconds);

            if (ImGui.BeginMainMenuBar())
            {
                ImGui.Text(_fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms"));

                var cameraPosition = _renderer.Scene.Camera.Position.ToString();

                ImGui.TextUnformatted($"Camera Position: {cameraPosition} Camera Angles: Pitch {_renderer.Scene.Camera.Pitch} Yaw {_renderer.Scene.Camera.Yaw}");

                ImGui.EndMainMenuBar();
            }

            _netClient.SendServerMessages(_netClient.Server);
        }

        public void Draw()
        {
            _renderer.Draw();
        }
    }
}
