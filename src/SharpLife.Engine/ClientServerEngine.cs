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
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using SharpLife.CommandSystem;
using SharpLife.Engine.Configuration;
using SharpLife.Engine.Shared.Loop;
using SharpLife.Engine.Shared.UI;
using SharpLife.Engine.Utility;
using SharpLife.FileSystem;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace SharpLife.Engine
{
    /// <summary>
    /// A client-server based engine
    /// Can host clients, dedicated servers and clients running listen servers
    /// </summary>
    internal class ClientServerEngine : IEngine, IEngineLoop
    {
        private static readonly List<string> CommandLineKeyPrefixes = new List<string> { "-", "+" };

        public ICommandLine CommandLine { get; private set; }

        public IFileSystem FileSystem { get; private set; }

        public string GameDirectory { get; private set; }

        public IConCommandSystem CommandSystem { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public DateTimeOffset BuildDate { get; private set; }

        private bool _exiting;

        public bool Exiting
        {
            get => _exiting;

            set
            {
                //Don't allow continuing loop once exit has been signalled
                if (!_exiting)
                {
                    _exiting = value;
                }
            }
        }

        private EngineConfiguration _engineConfiguration;

        private GameConfiguration _gameConfiguration;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private readonly FrameTimeAverager _fta = new FrameTimeAverager(0.666);

        private readonly double _desiredFrameLengthSeconds = 1.0 / 60.0;

        private IWindow _window;

        private Renderer.Renderer _renderer;

        public void CreateUserInterface()
        {
            if (UserInterface == null)
            {
                UserInterface = new UserInterface(FileSystem, this);
            }
        }

        public void Run(string[] args)
        {
            CommandLine = new CommandLine(args, CommandLineKeyPrefixes);

            GameDirectory = CommandLine.GetValue("-game");

            //This can't actually happen since SharpLife loads from its own directory, so unless somebody placed the installation in the default game directory this isn't an issue
            //It's an easy way to verify that nothing went wrong during user setup though
            if (GameDirectory == null)
            {
                throw new InvalidOperationException("No game directory specified, cannot continue");
            }

            _engineConfiguration = LoadEngineConfiguration(GameDirectory);

            Log.Logger = CreateLogger(GameDirectory);

            SystemInitialize();

            HostInitialize(GameDirectory);

            _window.Center();

            long previousFrameTicks = 0;

            while (!_exiting)
            {
                var currentFrameTicks = _stopwatch.ElapsedTicks;
                double deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;

                while (deltaSeconds < _desiredFrameLengthSeconds)
                {
                    currentFrameTicks = _stopwatch.ElapsedTicks;
                    deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;
                }

                previousFrameTicks = currentFrameTicks;

                UserInterface.SleepUntilInput(0);

                Update((float)deltaSeconds);

                if (_exiting)
                {
                    break;
                }

                _renderer.Draw();
            }

            SystemShutdown();
        }

        private void Update(float deltaSeconds)
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

        private static EngineConfiguration LoadEngineConfiguration(string gameDirectory)
        {
            EngineConfiguration engineConfiguration;

            using (var stream = new FileStream($"{gameDirectory}/cfg/SharpLife-Engine.xml", FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(EngineConfiguration));

                engineConfiguration = (EngineConfiguration)serializer.Deserialize(stream);
            }

            if (string.IsNullOrWhiteSpace(engineConfiguration.DefaultGame))
            {
                throw new InvalidOperationException("Default game must be specified");
            }

            if (string.IsNullOrWhiteSpace(engineConfiguration.DefaultGameName))
            {
                throw new InvalidOperationException("Default game name must be specified");
            }

            //Use a default configuration if none was provided
            if (engineConfiguration.LoggingConfiguration == null)
            {
                engineConfiguration.LoggingConfiguration = new LoggingConfiguration();
            }

            return engineConfiguration;
        }

        private ILogger CreateLogger(string gameDirectory)
        {
            var config = new LoggerConfiguration();

            ITextFormatter formatter = null;

            switch (_engineConfiguration.LoggingConfiguration.LogFormat)
            {
                case LoggingConfiguration.Format.Text:
                    {
                        formatter = new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null);
                        break;
                    }

                case LoggingConfiguration.Format.CompactJSON:
                    {
                        formatter = new CompactJsonFormatter();
                        break;
                    }
            }

            //Invalid config setting for RetainedFileCountLimit will throw
            config
                .WriteTo.File(formatter, $"{gameDirectory}/logs/engine.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: _engineConfiguration.LoggingConfiguration.RetainedFileCountLimit);

            return config.CreateLogger();
        }

        private void SystemInitialize()
        {
            _stopwatch.Start();

            //Disable to prevent debugger from shutting down the game
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            if (CommandLine.Contains("-noontop"))
            {
                SDL.SDL_SetHint(SDL.SDL_HINT_ALLOW_TOPMOST, "0");
            }

            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_XRANDR, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_XVIDMODE, "1");

            SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);
        }

        private void SystemShutdown()
        {
            SDL.SDL_Quit();
        }

        private void HostInitialize(string gameDirectory)
        {
            CommandSystem = new ConCommandSystem(CommandLine);

            FileSystem = new DiskFileSystem();

            SetupFileSystem(gameDirectory);

            try
            {
                using (var stream = FileSystem.OpenRead("cfg/SharpLife-Game.xml"))
                {
                    var serializer = new XmlSerializer(typeof(GameConfiguration));

                    _gameConfiguration = (GameConfiguration)serializer.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Couldn't load game configuration file\n{e}");

                throw;
            }

            var gameWindowName = _engineConfiguration.DefaultGameName;

            if (!string.IsNullOrWhiteSpace(_gameConfiguration.GameName))
            {
                gameWindowName = _gameConfiguration.GameName;
            }

            UserInterface = new UserInterface(FileSystem, this);

            _window = UserInterface.CreateMainWindow(gameWindowName, CommandLine.Contains("-noborder") ? SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS : 0);

            _window.Center();

            _renderer = new Renderer.Renderer(
                _window.WindowHandle,
                _window.GLContextHandle,
                FileSystem,
                UserInterface.WindowManager.InputSystem,
                Framework.Path.EnvironmentMaps,
                Framework.Path.Shaders);

            _window.Resized += _renderer.WindowResized;

            //TODO: initialize subsystems

            //Get the build date from the generated resource file
            var assembly = typeof(ClientServerEngine).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.BuildDate.txt")))
            {
                string buildTimestamp = reader.ReadToEnd();

                BuildDate = DateTimeOffset.Parse(buildTimestamp);

                Console.WriteLine($"Exe: {BuildDate.ToString("HH:mm:ss MMM dd yyyy")}");
            }

            CommandSystem.QueueCommands(CommandSource.Local, $"exec {_engineConfiguration.DefaultGame}.rc");
        }

        private void SetupFileSystem(string gameDirectory)
        {
            //Note: the engine has no-Steam directory paths used for testing, but since this is Steam-only, we won't add those
            FileSystem.RemoveAllSearchPaths();

            //Strip off the exe name
            var baseDir = Path.GetDirectoryName(CommandLine[0]);

            FileSystem.SetupFileSystem(
                baseDir,
                _engineConfiguration.DefaultGame,
                gameDirectory,
                Framework.DefaultLanguage,
                Framework.DefaultLanguage,
                false,
                !CommandLine.Contains("-nohdmodels") && _engineConfiguration.EnableHDModels,
                CommandLine.Contains("-addons") || _engineConfiguration.EnableAddonsFolder);
        }
    }
}
