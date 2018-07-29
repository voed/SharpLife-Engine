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
using System;
using System.Collections.Generic;

namespace SharpLife.CommandSystem
{
    public class CommandSystem : ICommandSystem
    {
        public int QueuedCommandCount => _commandsToExecute.Count;

        public IReadOnlyDictionary<string, string> Aliases => _aliases;

        internal readonly ILogger _logger;

        private readonly IList<Delegates.CommandFilter> _commandFilters = new List<Delegates.CommandFilter>();

        private readonly IDictionary<string, BaseCommand> _commands = new Dictionary<string, BaseCommand>();

        /// <summary>
        /// Commands that have been queued up for execution
        /// </summary>
        private readonly List<ICommandArgs> _commandsToExecute = new List<ICommandArgs>();

        private readonly Dictionary<string, string> _aliases = new Dictionary<string, string>();

        /// <summary>
        /// If true, <see cref="Execute"/> will stop executing commands and returns
        /// The next call to <see cref="Execute"/> will execute until the next wait or until all commands have been executed
        /// </summary>
        private bool _wait;

        /// <summary>
        /// Creates a new command system
        /// </summary>
        /// <param name="logger"></param>
        public CommandSystem(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        public void InsertCommands(CommandSource commandSource, string commandText, int index = 0)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            if (index < 0 || index > _commandsToExecute.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var commands = CommandUtils.ParseCommands(commandSource, commandText);

            var startIndex = index;

            commands.ForEach(command => _commandsToExecute.Insert(startIndex++, command));
        }

        public void SetAlias(string aliasName, string commandText)
        {
            if (aliasName == null)
            {
                throw new ArgumentNullException(nameof(aliasName));
            }

            if (string.IsNullOrWhiteSpace(aliasName))
            {
                throw new ArgumentException(nameof(aliasName));
            }

            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            if (!string.IsNullOrEmpty(commandText))
            {
                _aliases[aliasName] = commandText;
            }
            else
            {
                //Remove empty aliases to save memory
                _aliases.Remove(aliasName);
            }
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
    }
}
