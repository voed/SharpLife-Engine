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

using SharpLife.Engine.CommandSystem;
using SharpLife.Engine.Configuration;
using SharpLife.Engine.Utility;
using SharpLife.Engine.Video;
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
    public sealed class EngineHost
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

        private string SharpLifeGameDirectory;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private ConCommandSystem _conCommandSystem;

        private Window _window;

        public void Start(string[] args, HostType type)
        {
            HostType = type;

            _commandLine = new CommandLine(args, CommandLineKeyPrefixes);

            SharpLifeGameDirectory = _commandLine.GetValue("-game");

            //This can't actually happen since SharpLife loads from its own directory, so unless somebody placed the installation in the default game directory this isn't an issue
            //It's an easy way to verify that nothing went wrong during user setup though
            if (SharpLifeGameDirectory == null)
            {
                throw new InvalidOperationException("No game directory specified, cannot continue");
            }

            SystemInitialize();

            HostInitialize();
        }

        private void SystemInitialize()
        {
            _stopwatch.Start();
        }

        private void HostInitialize()
        {
            _conCommandSystem = new ConCommandSystem(_commandLine);

            using (var stream = new FileStream($"{SharpLifeGameDirectory}/cfg/SharpLife-Engine.xml", FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(EngineConfiguration));

                EngineConfiguration = (EngineConfiguration)serializer.Deserialize(stream);
            }

            if (string.IsNullOrWhiteSpace(EngineConfiguration.DefaultGame))
            {
                throw new InvalidOperationException("Default game must be specified");
            }

            try
            {
                using (var stream = new FileStream($"{SharpLifeGameDirectory}/cfg/SharpLife-Game.xml", FileMode.Open))
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

            _window = new Window(_commandLine, GameConfiguration);

            _window.CreateGameWindow();

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
    }
}
