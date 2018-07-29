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
using SharpLife.CommandSystem.Commands;
using SharpLife.FileSystem;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpLife.CommandSystem
{
    public class CommandSystem : ICommandSystem
    {
        internal readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;

        private readonly IList<Delegates.CommandFilter> _commandFilters = new List<Delegates.CommandFilter>();

        private readonly IDictionary<string, BaseCommand> _commands = new Dictionary<string, BaseCommand>();

        /// <summary>
        /// Commands that have been queued up for execution
        /// </summary>
        private readonly List<ICommandArgs> _commandsToExecute = new List<ICommandArgs>();

        private readonly IDictionary<string, string> _aliases = new Dictionary<string, string>();

        /// <summary>
        /// If true, <see cref="Execute"/> will stop executing commands and returns
        /// The next call to <see cref="Execute"/> will execute until the next wait or until all commands have been executed
        /// </summary>
        private bool _wait;

        /// <summary>
        /// Creates a new command system
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="fileSystem"></param>
        /// <param name="commandLine"></param>
        /// <param name="gameConfigPathIDs">The game config path IDs to use for the exec command</param>
        public CommandSystem(ILogger logger, IFileSystem fileSystem, ICommandLine commandLine, IReadOnlyList<string> gameConfigPathIDs)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            if (commandLine == null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            if (gameConfigPathIDs == null)
            {
                throw new ArgumentNullException(nameof(gameConfigPathIDs));
            }

            if (gameConfigPathIDs.Count == 0)
            {
                throw new ArgumentException("You must provide at least one game config path ID", nameof(gameConfigPathIDs));
            }

            //TODO: consider moving this out so users can define these
            _commandFilters.Add((commandToExecute, command) =>
            {
                if (command.CommandSource == CommandSource.Client && (commandToExecute.Flags & CommandFlags.ServerOnly) != 0)
                {
                    return false;
                }

                _logger.Error("Clients cannot execute server commands");

                return true;
            });

            _commandFilters.Add((commandToExecute, command) =>
            {
                //TODO: determine if this is the client
                if (command.CommandSource != CommandSource.Local && (commandToExecute.Flags & CommandFlags.ClientOnly) != 0)
                {
                    return false;
                }

                _logger.Error("Servers cannot execute client commands");

                return true;
            });

            RegisterCommand(new CommandInfo("stuffcmds", arguments =>
            {
                if (arguments.Count > 0)
                {
                    _logger.Information("stuffcmds : execute command line parameters");
                    return;
                }

                var cmdIndex = 0;

                for (var i = 0; i < commandLine.Count - 1; ++i)
                {
                    var key = commandLine[i];

                    if (key.StartsWith("+"))
                    {
                        //Grab all arguments until the next key
                        var values = commandLine.GetValues(key);

                        _commandsToExecute.Insert(cmdIndex++, new CommandArgs(CommandSource.Local, key.Substring(1), values));

                        i += values.Count;
                    }
                }
            })
            .WithHelpInfo("Stuffs all command line arguments that contain commands into the command queue"));

            RegisterCommand(new CommandInfo("exec", arguments =>
            {
                if (arguments.Count < 1)
                {
                    _logger.Information("exec <filename> : execute a script file");
                    return;
                }

                var fileName = arguments[0];

                if (fileName.IndexOfAny(new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar,
                    Path.VolumeSeparatorChar,
                    ':',
                    '~'
                }) != -1
                || fileName.Contains(".."))
                {
                    _logger.Error($"exec {fileName}: invalid path.");
                    return;
                }

                if (fileName.IndexOf('.') != fileName.LastIndexOf('.'))
                {
                    _logger.Error($"exec {fileName}: invalid filename.");
                    return;
                }

                var extension = Path.GetExtension(fileName);

                //TODO: need to define these extensions
                if (extension != ".cfg" && extension != ".rc")
                {
                    _logger.Error($"exec {fileName}: not a .cfg or .rc file");
                    return;
                }

                try
                {
                    var succeeded = false;

                    foreach (var pathID in gameConfigPathIDs)
                    {
                        if (_fileSystem.Exists(fileName, pathID))
                        {
                            var text = _fileSystem.ReadAllText(fileName, pathID);

                            _logger.Debug($"execing {arguments[0]}");

                            InsertCommands(CommandSource.Local, text);

                            succeeded = true;
                            break;
                        }
                    }

                    if (!succeeded)
                    {
                        throw new FileNotFoundException("Couldn't execute file", fileName);
                    }
                }
                catch (Exception)
                {
                    _logger.Error($"Couldn't exec {arguments[0]}");
                }
            })
            .WithHelpInfo("Executes a file containing commands"));

            RegisterCommand(new CommandInfo("echo", arguments => _logger.Information(arguments.ArgumentsString)).WithHelpInfo("Echoes the arguments to the console"));

            RegisterCommand(new CommandInfo("alias", arguments =>
            {
                if (arguments.Count == 0)
                {
                    _logger.Information("Current alias commands:");
                    foreach (var entry in _aliases)
                    {
                        _logger.Information($"{entry.Key}: {entry.Value}\n");
                    }
                    return;
                }

                // if the alias already exists, reuse it
                _aliases[arguments[0]] = arguments.ArgumentsAsString(1);
            })
            .WithHelpInfo("Aliases a command to a name"));

            //TODO: move out of the command system
            RegisterCommand(new CommandInfo("cmd", arguments => ForwardToServer(arguments, false)
            ).WithHelpInfo("Send the entire command line over to the server"));

            RegisterCommand(new CommandInfo("wait", _ => _wait = true)
                .WithHelpInfo("Delay execution of remaining commands by one frame"));
        }

        public TCommand FindCommand<TCommand>(string name)
            where TCommand : class, IBaseCommand
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_commands.TryGetValue(name, out var command))
            {
                return command as TCommand;
            }

            return null;
        }

        public ICommand RegisterCommand(CommandInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (_commands.ContainsKey(info.Name))
            {
                throw new ArgumentException($"Cannot add duplicate command \"{info.Name}\"");
            }

            var command = new Command(this, info.Name, info.Executors, info.Flags, info.HelpInfo);

            _commands.Add(command.Name, command);

            return command;
        }

        public IVariable RegisterVariable(VariableInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (_commands.ContainsKey(info.Name))
            {
                throw new ArgumentException($"Cannot add duplicate command \"{info.Name}\"");
            }

            Variable variable = null;

            if (info.StringValue != null)
            {
                variable = new Variable(this, info.Name, info.StringValue, info.Flags, info.HelpInfo, info.Filters, info.ChangeHandlers);
            }
            else if (info.FloatValue != null)
            {
                variable = new Variable(this, info.Name, info.FloatValue.Value, info.Flags, info.HelpInfo, info.Filters, info.ChangeHandlers);
            }
            else if (info.IntegerValue != null)
            {
                variable = new Variable(this, info.Name, info.IntegerValue.Value, info.Flags, info.HelpInfo, info.Filters, info.ChangeHandlers);
            }
            else
            {
                throw new ArgumentException("Command variables must have a value specified", nameof(info));
            }

            _commands.Add(variable.Name, variable);

            return variable;
        }

        public void QueueCommands(CommandSource commandSource, string commandText)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            var commands = CommandUtils.ParseCommands(commandSource, commandText);

            commands.ForEach(_commandsToExecute.Add);
        }

        public void InsertCommands(CommandSource commandSource, string commandText)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            var commands = CommandUtils.ParseCommands(commandSource, commandText);

            var startIndex = 0;

            commands.ForEach(command => _commandsToExecute.Insert(startIndex++, command));
        }

        public void Execute()
        {
            while (!_wait && _commandsToExecute.Count > 0)
            {
                var commandArgs = _commandsToExecute[0];
                _commandsToExecute.RemoveAt(0);

                if (_commands.TryGetValue(commandArgs.Name, out var command))
                {
                    try
                    {
                        command.OnCommand(commandArgs);
                    }
                    catch (InvalidCommandSyntaxException e)
                    {
                        _logger.Information(e.Message);
                    }
                }
                //This is different from the original; there aliases are checked before cvars
                else if (_aliases.TryGetValue(commandArgs.Name, out var aliasedCommand))
                {
                    InsertCommands(CommandSource.Local, aliasedCommand);
                }
                else
                {
                    _logger.Information($"Could not find command {commandArgs.Name}");
                }
            }

            if (_wait)
            {
                _wait = false;
            }
        }

        private void ForwardToServer(ICommandArgs command, bool includeCommandName)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            //TODO: implement
        }

        public void ForwardToServer(ICommandArgs command)
        {
            ForwardToServer(command, true);
        }
    }
}
