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

using System;

namespace SharpLife.CommandSystem.Commands
{
    internal abstract class BaseCommand : IBaseCommand
    {
        protected readonly CommandContext _commandContext;

        public string Name { get; }

        public CommandFlags Flags { get; }

        public string HelpInfo { get; }

        protected BaseCommand(CommandContext commandContext, string name, CommandFlags flags = CommandFlags.None, string helpInfo = "")
        {
            _commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext));

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            Name = name;
            Flags = flags;
            HelpInfo = helpInfo ?? throw new ArgumentNullException(nameof(helpInfo));
        }

        /// <summary>
        /// Handles a command invocation with the given arguments
        /// </summary>
        /// <param name="command"></param>
        /// <exception cref="InvalidCommandSyntaxException">When the command is invoked with the wrong syntax</exception>
        internal abstract void OnCommand(ICommandArgs command);
    }
}
