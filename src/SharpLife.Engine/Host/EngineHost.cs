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

using SDL2;
using Serilog;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using SharpLife.Engine.CommandSystem;
using SharpLife.Engine.Configuration;
using SharpLife.Engine.FileSystem;
using SharpLife.Engine.Loop;
using SharpLife.Engine.Utility;
using SharpLife.Engine.Video;
using SharpLife.FileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace SharpLife.Engine.Host
{
    /// <summary>
    /// Handles engine hosting, startup
    /// </summary>
    public sealed class EngineHost : IEngineLoop
    {
        private static readonly List<string> CommandLineKeyPrefixes = new List<string> { "-", "+" };

        public HostType HostType { get; private set; }

        public bool IsClient => HostType == HostType.Client;

        public bool IsDedicatedServer => HostType == HostType.DedicatedServer;

        //TODO
        public bool IsListenServer => HostType == HostType.Client;

        public DateTimeOffset BuildDate { get; private set; }

        private EngineConfiguration EngineConfiguration { get; set; }

        private GameConfiguration GameConfiguration { get; set; }

        private ICommandLine _commandLine;

        private IFileSystem _fileSystem;

        private string SharpLifeGameDirectory;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private ConCommandSystem _conCommandSystem;

        private Window _window;

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

        public void Start(string[] args, HostType type)
        {
            HostType = type;

            try
            {
                StartHost(args);
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
            catch (Exception e)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
            {
                //Log first, in case user terminates program while messagebox is open
                Log.Logger?.Error(e, "A fatal error occurred");

                //Display an error message
                SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "SharpLife error", e.Message, IntPtr.Zero);

                throw;
            }
        }

        private void StartHost(string[] args)
        {
            _commandLine = new CommandLine(args, CommandLineKeyPrefixes);

            SharpLifeGameDirectory = _commandLine.GetValue("-game");

            //This can't actually happen since SharpLife loads from its own directory, so unless somebody placed the installation in the default game directory this isn't an issue
            //It's an easy way to verify that nothing went wrong during user setup though
            if (SharpLifeGameDirectory == null)
            {
                throw new InvalidOperationException("No game directory specified, cannot continue");
            }

            EngineConfiguration = LoadEngineConfiguration(SharpLifeGameDirectory);

            Log.Logger = CreateLogger();

            SystemInitialize();

            HostInitialize();

            _window.CenterWindow();

            while (!_exiting)
            {
                _window.SleepUntilInput(0);
            }

            SystemShutdown();
        }

        private ILogger CreateLogger()
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
                .WriteTo.File(formatter, $"{SharpLifeGameDirectory}/logs/engine.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: EngineConfiguration.LoggingConfiguration.RetainedFileCountLimit);

            return config.CreateLogger();
        }

        private void SystemInitialize()
        {
            _stopwatch.Start();

            //Disable to prevent debugger from shutting down the game
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            if (_commandLine.Contains("-noontop"))
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

        private void HostInitialize()
        {
            _conCommandSystem = new ConCommandSystem(_commandLine);

            _fileSystem = new DiskFileSystem();

            SetupFileSystem();

            try
            {
                using (var stream = _fileSystem.OpenRead($"{SharpLifeGameDirectory}/cfg/SharpLife-Game.xml"))
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

            _window = new Window(_commandLine, _fileSystem, this, EngineConfiguration, GameConfiguration);

            //TODO: initialize subsystems

            //Get the build date from the generated resource file
            var assembly = typeof(EngineHost).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.BuildDate.txt")))
            {
                string buildTimestamp = reader.ReadToEnd();

                BuildDate = DateTimeOffset.Parse(buildTimestamp);

                Console.WriteLine($"Exe: {BuildDate.ToString("HH:mm:ss MMM dd yyyy")}");
            }

            _conCommandSystem.QueueCommands(CommandSource.Local, $"exec {EngineConfiguration.DefaultGame}.rc");
        }

        private void SetupFileSystem()
        {
            //Note: the engine has no-Steam directory paths used for testing, but since this is Steam-only, we won't add those
            _fileSystem.RemoveAllSearchPaths();

            //Strip off the exe name
            var baseDir = Path.GetDirectoryName(_commandLine[0]);

            string defaultGame = EngineConfiguration.DefaultGame;

            var gameDir = SharpLifeGameDirectory;

            //TODO: get language from SteamWorks
            const string language = Framework.DefaultLanguage;

            const bool addLanguage = language != Framework.DefaultLanguage;

            //TODO: get from SteamWorks
            const bool lowViolence = false;

            var hdModels = !_commandLine.Contains("-nohdmodels") && EngineConfiguration.EnableHDModels;

            var addons = _commandLine.Contains("-addons") || EngineConfiguration.EnableAddonsFolder;

            void AddGameDirectories(string gameDirectoryName, string pathID)
            {
                if (lowViolence)
                {
                    _fileSystem.AddSearchPath($"{baseDir}/{gameDirectoryName}{FileSystemConstants.Suffixes.LowViolence}", pathID, false);
                }

                if (addons)
                {
                    _fileSystem.AddSearchPath($"{baseDir}/{gameDirectoryName}{FileSystemConstants.Suffixes.Addon}", pathID, false);
                }

                if (addLanguage)
                {
                    _fileSystem.AddSearchPath($"{baseDir}/{gameDirectoryName}_{language}", pathID, false);
                }

                if (hdModels)
                {
                    _fileSystem.AddSearchPath($"{baseDir}/{gameDirectoryName}{FileSystemConstants.Suffixes.HD}", pathID, false);
                }
            }

            AddGameDirectories(gameDir, FileSystemConstants.PathID.Game);

            _fileSystem.AddSearchPath($"{baseDir}/{gameDir}", FileSystemConstants.PathID.Game);
            _fileSystem.AddSearchPath($"{baseDir}/{gameDir}", FileSystemConstants.PathID.GameConfig);

            _fileSystem.AddSearchPath($"{baseDir}/{gameDir}{FileSystemConstants.Suffixes.Downloads}", FileSystemConstants.PathID.GameDownload);

            AddGameDirectories(defaultGame, FileSystemConstants.PathID.DefaultGame);

            _fileSystem.AddSearchPath(baseDir, FileSystemConstants.PathID.Base);

            _fileSystem.AddSearchPath($"{baseDir}/{defaultGame}", FileSystemConstants.PathID.Game, false);

            _fileSystem.AddSearchPath($"{baseDir}/{FileSystemConstants.PlatformDirectory}", FileSystemConstants.PathID.Platform);
        }
    }
}
