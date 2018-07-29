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
    /// Provides access to the command system
    /// Direct registration of command instances is not permitted, instead you provide info objects that describe the commands
    /// This prevents users from needing to know about the concrete layout of the command classes
    /// </summary>
    public interface ICommandSystem
    {
        TCommand FindCommand<TCommand>(string name) where TCommand : class, IBaseCommand;

        ICommand RegisterCommand(CommandInfo info);

        IVariable RegisterVariable(VariableInfo info);

        void QueueCommands(CommandSource commandSource, string commandText);

        void Execute();
    }
}
