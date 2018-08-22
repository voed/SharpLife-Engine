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
using SharpLife.Engine.Shared.CommandSystem;
using SharpLife.Engine.Shared.Configuration;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.Events;
using SharpLife.Engine.Shared.Logging;
using SharpLife.Engine.Shared.Loop;
using SharpLife.Engine.Shared.Maps;
using SharpLife.Engine.Shared.UI;
using SharpLife.Engine.Shared.Utility;
using SharpLife.FileSystem;
using SharpLife.Networking.Shared;
using SharpLife.Utility;
using SharpLife.Utility.Events;
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

        public ICommandSystem CommandSystem { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        private EngineTime EngineTime { get; } = new EngineTime();

        IEngineTime IEngine.EngineTime => EngineTime;

        public IMapManager MapManager { get; private set; }

        public IEventSystem EventSystem { get; } = new EventSystem();

        public EngineConfiguration EngineConfiguration { get; private set; }

        public DateTimeOffset BuildDate { get; private set; }

        public bool IsDedicatedServer => _hostType == HostType.DedicatedServer;

        public bool IsServerActive => _server?.Active == true;

        public ForwardingTextWriter LogTextWriter { get; } = new ForwardingTextWriter();

        private HostType _hostType;

        //Internal so the host can access it if needed
        internal ILogger Logger { get; private set; }

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
                UserInterface = new UserInterface(Logger, FileSystem, this, CommandLine.Contains("-noontop"));
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

            Log.Logger = Logger = CreateLogger(GameDirectory);

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

            _server?.RunFrame(deltaSeconds);

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

            config.MinimumLevel.Verbose();

            ITextFormatter fileFormatter = null;

            switch (EngineConfiguration.LoggingConfiguration.LogFormat)
            {
                case LoggingConfiguration.Format.Text:
                    {
                        fileFormatter = new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null);
                        break;
                    }

                case LoggingConfiguration.Format.CompactJSON:
                    {
                        fileFormatter = new CompactJsonFormatter();
                        break;
                    }
            }

            //Invalid config setting for RetainedFileCountLimit will throw
            config
                .WriteTo.File(fileFormatter, $"{gameDirectory}/logs/engine.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: EngineConfiguration.LoggingConfiguration.RetainedFileCountLimit);

            //Use basic formatting for console output
            var logFormatter = new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}", null);

            config.WriteTo.TextWriter(logFormatter, LogTextWriter);

            return config.CreateLogger();
        }

        private void Initialize(string gameDirectory, HostType hostType)
        {
            EngineTime.Start();

            EventUtils.RegisterEvents(EventSystem, new EngineEvents());

            FileSystem = new DiskFileSystem();

            SetupFileSystem(gameDirectory);

            CommandSystem = new CommandSystem.CommandSystem(Logger);

            CommonCommands.AddStuffCmds(CommandSystem.SharedContext, Logger, CommandLine);
            CommonCommands.AddExec(CommandSystem.SharedContext, Logger, FileSystem, ExecPathIDs);
            CommonCommands.AddEcho(CommandSystem.SharedContext, Logger);
            CommonCommands.AddAlias(CommandSystem.SharedContext, Logger);

            //Get the build date from the generated resource file
            var assembly = typeof(ClientServerEngine).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.BuildDate.txt")))
            {
                string buildTimestamp = reader.ReadToEnd();

                BuildDate = DateTimeOffset.Parse(buildTimestamp);

                Logger.Information($"Exe: {BuildDate.ToString("HH:mm:ss MMM dd yyyy")}");
            }

            if (hostType == HostType.Client)
            {
                _client = new EngineClientHost(this, Logger);
            }

            if (hostType == HostType.DedicatedServer)
            {
                CreateServer();
            }

            MapManager = new MapManager(Logger, FileSystem, Framework.Path.Maps, Framework.Extension.BSP);

            CommandSystem.SharedContext.RegisterCommand(new CommandInfo("map", StartNewMap).WithHelpInfo("Loads the specified map"));

            //TODO: initialize subsystems

            //Only one of these will exist right now
            _client?.CommandContext.QueueCommands($"exec {EngineConfiguration.DefaultGame}.rc");
            _server?.CommandContext.QueueCommands($"exec {EngineConfiguration.DefaultGame}.rc");
        }

        private void Shutdown()
        {
            _server?.Shutdown();
            _client?.Shutdown();

            UserInterface?.Shutdown();

            EventUtils.UnregisterEvents(EventSystem, new EngineEvents());
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

        private void CreateServer()
        {
            if (_server == null)
            {
                _server = new EngineServerHost(this, Logger);
            }
        }

        /// <summary>
        /// Start a new map, loading entities from the map entity data string
        /// </summary>
        /// <param name="command"></param>
        private void StartNewMap(ICommandArgs command)
        {
            if (command.Count == 0)
            {
                Logger.Information("map <levelname> : changes server to specified map");
                return;
            }

            _client?.Disconnect(false);

            var mapName = command[0];

            //Remove BSP extension
            if (mapName.EndsWith(FileExtensionUtils.AsExtension(Framework.Extension.BSP)))
            {
                mapName = Path.GetFileNameWithoutExtension(mapName);
            }

            CreateServer();

            EventSystem.DispatchEvent(EngineEvents.EngineNewMapRequest);

            if (!MapManager.IsMapValid(mapName))
            {
                Logger.Error($"map change failed: '{mapName}' not found on server.");
                return;
            }

            _server.Stop();

            EventSystem.DispatchEvent(EngineEvents.EngineStartingServer);

            const ServerStartFlags flags = ServerStartFlags.None;

            if (!_server.Start(mapName, null, flags))
            {
                return;
            }

            FinishLoadMap(null, flags);

            //Listen server hosts need to connect to their own server
            if (!IsDedicatedServer)
            {
                _client.CommandContext.QueueCommands($"connect {NetAddresses.Local}");
            }
        }

        /// <summary>
        /// Finishes loading a map and activates the server
        /// </summary>
        /// <param name="startSpot"></param>
        /// <param name="flags"></param>
        private void FinishLoadMap(string startSpot = null, ServerStartFlags flags = ServerStartFlags.None)
        {
            _server.InitializeMap(flags);

            _server.Activate();
        }

        public void EndGame(string reason)
        {
            if (reason == null)
            {
                throw new ArgumentNullException(nameof(reason));
            }

            Logger.Debug($"Host_EndGame: {reason}");

            StopServer();

            //Disconnected by server
            _client?.Disconnect(false);
        }

        public void StopServer()
        {
            if (_server?.Active == true)
            {
                _server.Stop();
            }
        }
    }
}
