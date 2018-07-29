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
    public sealed class CommandSystem : ICommandSystem
    {
        public ICommandQueue Queue => _queue;

        public ICommandContext SharedContext => _sharedContext;

        internal readonly ILogger _logger;

        private readonly CommandQueue _queue;

        private readonly List<CommandContext> _commandContexts = new List<CommandContext>();

        private readonly SharedCommandContext _sharedContext;

        /// <summary>
        /// Creates a new command system
        /// </summary>
        /// <param name="logger"></param>
        public CommandSystem(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _queue = new CommandQueue(_logger);

            _sharedContext = new SharedCommandContext(_logger, this, "SharedContext");

            _commandContexts.Add(_sharedContext);

            //Add as a shared command
            SharedContext.RegisterCommand(new CommandInfo("wait", _ => _queue.Wait = true)
                .WithHelpInfo("Delay execution of remaining commands until the next execution"));
        }

        public ICommandContext CreateContext(string name, object tag = null, string protectedVariableChangeString = null)
        {
            var context = new CommandContext(_logger, this, name, tag, protectedVariableChangeString);

            _commandContexts.Add(context);

            //Add all existing shared commands
            foreach (var command in _sharedContext.Commands.Values)
            {
                context.AddSharedCommand((BaseCommand)command);
            }

            return context;
        }

        private void InternalDestroyContext(ICommandContext context)
        {
            var internalContext = (CommandContext)context;

            if (!_commandContexts.Contains(internalContext))
            {
                throw new ArgumentException(nameof(context));
            }

            _commandContexts.Remove(internalContext);

            //TODO: mark context as destroyed to prevent command queueing
        }

        public void DestroyContext(ICommandContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //Don't allow users to destroy the shared context
            if (ReferenceEquals(context, _sharedContext))
            {
                throw new ArgumentException(nameof(context));
            }

            InternalDestroyContext((CommandContext)context);
        }

        public void Execute()
        {
            _queue.Execute();
        }

        internal void OnSharedAddCommand(IBaseCommand command)
        {
            var internalCommand = (BaseCommand)command;

            foreach (var context in _commandContexts)
            {
                if (!ReferenceEquals(context, _sharedContext))
                {
                    context.AddSharedCommand(internalCommand);
                }
            }
        }
    }
}
