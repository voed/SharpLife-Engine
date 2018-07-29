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
using System.Collections.Generic;

namespace SharpLife.CommandSystem.Commands
{
    internal class Command : BaseCommand, ICommand
    {
        public event Delegates.CommandExecutor OnExecute;

        public Command(CommandContext commandContext, string name, IReadOnlyList<Delegates.CommandExecutor> executors, CommandFlags flags = CommandFlags.None, string helpInfo = "")
            : base(commandContext, name, flags, helpInfo)
        {
            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            foreach (var executor in executors)
            {
                OnExecute += executor;
            }
        }

        internal override void OnCommand(ICommandArgs command)
        {
            OnExecute?.Invoke(command);
        }
    }
}
