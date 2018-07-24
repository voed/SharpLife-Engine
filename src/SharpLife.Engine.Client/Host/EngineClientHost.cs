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
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.API.Game;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.ModUtils;
using SharpLife.Engine.Shared.UI;
using SharpLife.Utility;
using System;

namespace SharpLife.Engine.Client.Host
{
    public class EngineClientHost : IEngineClientHost
    {
        public IConCommandSystem CommandSystem => _engine.CommandSystem;

        private readonly IEngine _engine;

        private readonly IUserInterface _userInterface;

        private readonly IWindow _window;

        private readonly Renderer.Renderer _renderer;

        private readonly FrameTimeAverager _fta = new FrameTimeAverager(0.666);

        private ModData<IClientMod> _mod;

        public EngineClientHost(IEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

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
        }

        public void Update(float deltaSeconds)
        {
            _fta.AddTime(deltaSeconds);
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
        /// Connect to a server
        /// </summary>
        /// <param name="command"></param>
        private void Connect(ICommand command)
        {
            if (command.Count == 0)
            {
                Console.WriteLine("usage: connect <server>");
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

            Disconnect();

            //TODO: initialize client state

            //TODO: store server name

            //TODO: configure networking
        }

        public void Disconnect()
        {
            //TODO: implement
        }
    }
}
