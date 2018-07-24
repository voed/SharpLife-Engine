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

using Serilog;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.Client.Host;
using SharpLife.Engine.Server.Host;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.Configuration;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Loop;
using SharpLife.Engine.Shared.Maps;
using SharpLife.Engine.Shared.Networking;
using SharpLife.Engine.Shared.UI;
using SharpLife.Engine.Shared.Utility;
using SharpLife.FileSystem;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace SharpLife.Engine.Engines
{
    /// <summary>
    /// A client-server based engine
    /// Can host clients, dedicated servers and clients running listen servers
    /// </summary>
    internal class ClientServerEngine : IEngine, IEngineLoop
    {
        private static readonly List<string> CommandLineKeyPrefixes = new List<string> { "-", "+" };

        private static readonly List<string> ExecPathIDs = new List<string>
        {
            FileSystemConstants.PathID.GameConfig,
            FileSystemConstants.PathID.Game,
            FileSystemConstants.PathID.All
        };

        public ICommandLine CommandLine { get; private set; }

        public IFileSystem FileSystem { get; private set; }

        public string GameDirectory { get; private set; }

        public IConCommandSystem CommandSystem { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        private EngineTime EngineTime { get; } = new EngineTime();

        IEngineTime IEngine.EngineTime => EngineTime;

        public IMapManager MapManager { get; private set; }

        public EngineConfiguration EngineConfiguration { get; private set; }

        public GameConfiguration GameConfiguration { get; private set; }

        public DateTimeOffset BuildDate { get; private set; }

        public bool IsDedicatedServer => _hostType == HostType.Client;

        private HostType _hostType;

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

        private readonly double _desiredFrameLengthSeconds = 1.0 / 60.0;

        private IEngineClientHost _client;
        private IEngineServerHost _server;

        public IUserInterface CreateUserInterface()
        {
            if (UserInterface == null)
            {
                UserInterface = new UserInterface(FileSystem, this, CommandLine.Contains("-noontop"));
            }

            return UserInterface;
        }

        public void Run(string[] args, HostType hostType)
        {
            _hostType = hostType;

            CommandLine = new CommandLine(args, CommandLineKeyPrefixes);

            GameDirectory = CommandLine.GetValue("-game");

            //This can't actually happen since SharpLife loads from its own directory, so unless somebody placed the installation in the default game directory this isn't an issue
            //It's an easy way to verify that nothing went wrong during user setup though
            if (GameDirectory == null)
            {
                throw new InvalidOperationException("No game directory specified, cannot continue");
            }

            EngineConfiguration = LoadEngineConfiguration(GameDirectory);

            Log.Logger = CreateLogger(GameDirectory);

            Initialize(GameDirectory, hostType);

            _client?.PostInitialize();

            long previousFrameTicks = 0;

            while (!_exiting)
            {
                var currentFrameTicks = EngineTime.ElapsedTicks;
                double deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;

                while (deltaSeconds < _desiredFrameLengthSeconds)
                {
                    currentFrameTicks = EngineTime.ElapsedTicks;
                    deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;
                }

                previousFrameTicks = currentFrameTicks;

                UserInterface?.SleepUntilInput(0);

                Update((float)deltaSeconds);

                if (_exiting)
                {
                    break;
                }

                _client?.Draw();
            }

            Shutdown();
        }

        private void Update(float deltaSeconds)
        {
            CommandSystem.Execute();

            _client?.Update(deltaSeconds);
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

            switch (EngineConfiguration.LoggingConfiguration.LogFormat)
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
                retainedFileCountLimit: EngineConfiguration.LoggingConfiguration.RetainedFileCountLimit);

            return config.CreateLogger();
        }

        private void Initialize(string gameDirectory, HostType hostType)
        {
            EngineTime.Start();

            FileSystem = new DiskFileSystem();

            SetupFileSystem(gameDirectory);

            CommandSystem = new ConCommandSystem(FileSystem, CommandLine, ExecPathIDs);

            try
            {
                using (var stream = FileSystem.OpenRead("cfg/SharpLife-Game.xml"))
                {
                    var serializer = new XmlSerializer(typeof(GameConfiguration));

                    GameConfiguration = (GameConfiguration)serializer.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Couldn't load game configuration file\n{e}");

                throw;
            }

            //Get the build date from the generated resource file
            var assembly = typeof(ClientServerEngine).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.BuildDate.txt")))
            {
                string buildTimestamp = reader.ReadToEnd();

                BuildDate = DateTimeOffset.Parse(buildTimestamp);

                Console.WriteLine($"Exe: {BuildDate.ToString("HH:mm:ss MMM dd yyyy")}");
            }

            if (hostType == HostType.Client)
            {
                _client = new EngineClientHost(this);
            }

            _server = new EngineServerHost(this);

            //For listen servers, the server game assembly is created when the client actually starts a map
            if (hostType == HostType.DedicatedServer)
            {
                _server.LoadGameAssembly();
            }

            MapManager = new MapManager(FileSystem, Framework.Path.Maps, Framework.Extension.BSP);

            CommandSystem.RegisterConCommand(new ConCommandInfo("map", StartNewMap).WithHelpInfo("Loads the specified map"));

            //TODO: initialize subsystems

            CommandSystem.QueueCommands(CommandSource.Local, $"exec {EngineConfiguration.DefaultGame}.rc");
        }

        private void Shutdown()
        {
            _server.Shutdown();
            _client?.Shutdown();

            UserInterface?.Shutdown();
        }

        private void SetupFileSystem(string gameDirectory)
        {
            //Note: the engine has no-Steam directory paths used for testing, but since this is Steam-only, we won't add those
            FileSystem.RemoveAllSearchPaths();

            //Strip off the exe name
            var baseDir = Path.GetDirectoryName(CommandLine[0]);

            FileSystem.SetupFileSystem(
                baseDir,
                EngineConfiguration.DefaultGame,
                gameDirectory,
                Framework.DefaultLanguage,
                Framework.DefaultLanguage,
                false,
                !CommandLine.Contains("-nohdmodels") && EngineConfiguration.EnableHDModels,
                CommandLine.Contains("-addons") || EngineConfiguration.EnableAddonsFolder);
        }

        private void InitializeGameAssembly()
        {
            if (!_server.GameAssemblyLoaded)
            {
                CommandSystem.Execute();

                //TODO: configure networking

                _server.LoadGameAssembly();
            }
            else
            {
                Console.WriteLine("LoadGameAssembly called twice, skipping second call");
            }
        }

        /// <summary>
        /// Start a new map, loading entities from the map entity data string
        /// </summary>
        /// <param name="command"></param>
        private void StartNewMap(ICommand command)
        {
            if (command.CommandSource != CommandSource.Local)
            {
                return;
            }

            if (command.Count == 0)
            {
                Console.WriteLine("map <levelname> : changes server to specified map");
                return;
            }

            _client?.Disconnect();

            var mapName = command[0];

            //Remove BSP extension
            if (mapName.EndsWith(FileExtensionUtils.AsExtension(Framework.Extension.BSP)))
            {
                mapName = Path.GetFileNameWithoutExtension(mapName);
            }

            InitializeGameAssembly();

            if (!MapManager.IsMapValid(mapName))
            {
                Console.WriteLine($"map change failed: '{mapName}' not found on server.");
                return;
            }

            StartMap(mapName);
        }

        /// <summary>
        /// Starts a new server on the given map, optionally loading a saved game
        /// </summary>
        /// <param name="mapName"></param>
        /// <param name="startSpot"></param>
        /// <param name="flags"></param>
        private void StartMap(string mapName, string startSpot = null, ServerStartFlags flags = ServerStartFlags.None)
        {
            _server.Stop();

            if (!_server.Start(mapName, startSpot, flags))
            {
                return;
            }

            if ((flags & ServerStartFlags.LoadGame) != 0)
            {
                //TODO: load game
            }
            else
            {
                //TODO: initialize entities through game assembly
            }

            //Listen server hosts need to connect to their own server
            if (!IsDedicatedServer)
            {
                CommandSystem.QueueCommands(CommandSource.Local, $"connect {NetAddresses.Local}");
            }
        }
    }
}
