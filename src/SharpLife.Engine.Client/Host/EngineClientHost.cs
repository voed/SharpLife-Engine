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
using Lidgren.Network;
using SDL2;
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Engine.API.Game;
using SharpLife.Engine.Client.Networking;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Events;
using SharpLife.Engine.Shared.ModUtils;
using SharpLife.Engine.Shared.UI;
using SharpLife.Networking.Shared;
using SharpLife.Utility;
using SharpLife.Utility.Events;
using System;
using System.Net;

namespace SharpLife.Engine.Client.Host
{
    public class EngineClientHost : IEngineClientHost
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

        private NetworkClient _netClient;

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

            //TODO: need to delay this until user config has been processed
            CreateNetworkClient();
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
        }

        public void Draw()
        {
            _renderer.Draw();
        }

        /// <summary>
        /// (Re)creates the network client, using current client configuration values
        /// </summary>
        private void CreateNetworkClient()
        {
            //Disconnect any previous connection
            if (_netClient != null)
            {
                Disconnect(false);
            }

            //TODO: could combine the app identifier with the mod name to allow concurrent mod hosts
            //Not possible since the original engine launcher blocks launching multiple instances
            //Should be possible for servers though

            _netClient = new NetworkClient(
                _logger,
                NetConstants.AppIdentifier,
                _clientport.Integer,
                _cl_resend.Float,
                _cl_timeout.Float);
        }

        private void HandlePacket(NetIncomingMessage message)
        {
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    HandleStatusChanged(message);
                    break;

                case NetIncomingMessageType.UnconnectedData:
                    //TODO: implement
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

        private void HandleStatusChanged(NetIncomingMessage message)
        {
            var status = (NetConnectionStatus)message.ReadByte();

            string reason = message.ReadString();

            switch (status)
            {
                case NetConnectionStatus.Connected:
                    ConnectionStatus = ClientConnectionStatus.Connected;
                    break;

                case NetConnectionStatus.Disconnected:
                    {
                        if (ConnectionStatus != ClientConnectionStatus.NotConnected)
                        {
                            //Disconnected by server
                            _engine.EndGame("Server disconnected");
                            //TODO: discard remaining incoming packets?
                        }
                        break;
                    }
            }
        }

        private void HandleData(NetIncomingMessage message)
        {
            //TODO: implement
        }

        /// <summary>
        /// Connect to a server
        /// </summary>
        /// <param name="command"></param>
        private void Connect(ICommand command)
        {
            if (command.Count == 0)
            {
                _logger.Information("usage: connect <server>");
                return;
            }

            var name = command.ArgumentsString;

            Connect(name);
        }

        public void Connect(string address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Disconnect(false);

            EventSystem.DispatchEvent(EngineEvents.ClientStartConnect);

            //TODO: initialize client state

            CreateNetworkClient();

            _netClient.Start();

            ConnectionStatus = ClientConnectionStatus.Connecting;

            //Told to connect to listen server, translate address
            if (address == NetAddresses.Local)
            {
                address = NetConstants.LocalHost;
            }

            _netClient.Connect(address);
        }

        private void Disconnect(ICommand command)
        {
            Disconnect(true);
        }

        public void Disconnect(bool shutdownServer)
        {
            //Always dispatch even if we're not connected
            EventSystem.DispatchEvent(EngineEvents.ClientStartDisconnect);

            if (ConnectionStatus != ClientConnectionStatus.NotConnected)
            {
                //TODO: implement
                _netClient.Shutdown(NetMessages.ClientDisconnectMessage);

                //The client considers itself disconnected immediately
                ConnectionStatus = ClientConnectionStatus.NotConnected;

                EventSystem.DispatchEvent(EngineEvents.ClientDisconnectSent);
            }

            EventSystem.DispatchEvent(EngineEvents.ClientEndDisconnect);

            if (shutdownServer)
            {
                _engine.StopServer();
            }
        }
    }
}
