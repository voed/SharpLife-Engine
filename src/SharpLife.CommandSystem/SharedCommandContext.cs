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

using SharpLife.CommandSystem.Commands;

namespace SharpLife.CommandSystem
{
    /// <summary>
    /// Shared context that informs the command system of all command additions to add them to other contexts
    /// </summary>
    internal sealed class SharedCommandContext : CommandContext
    {
        public SharedCommandContext(Serilog.ILogger logger, CommandSystem commandSystem, string name, object tag = null)
            : base(logger, commandSystem, name, tag)
        {
        }

        public override ICommand RegisterCommand(CommandInfo info)
        {
            var command = base.RegisterCommand(info);

            _commandSystem.OnSharedAddCommand(command);

            return command;
        }

        public override IVariable RegisterVariable(VariableInfo info)
        {
            var command = base.RegisterVariable(info);

            _commandSystem.OnSharedAddCommand(command);

            return command;
        }
    }
}
