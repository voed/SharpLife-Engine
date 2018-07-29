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
using System.Collections.Generic;

namespace SharpLife.CommandSystem
{
    /// <summary>
    /// Provides access to the command system
    /// Direct registration of command instances is not permitted, instead you provide info objects that describe the commands
    /// This prevents users from needing to know about the concrete layout of the command classes
    /// </summary>
    public interface ICommandSystem
    {
        /// <summary>
        /// The number of commands that have been queued up
        /// </summary>
        int QueuedCommandCount { get; }

        IReadOnlyDictionary<string, string> Aliases { get; }

        TCommand FindCommand<TCommand>(string name) where TCommand : class, IBaseCommand;

        ICommand RegisterCommand(CommandInfo info);

        IVariable RegisterVariable(VariableInfo info);

        void QueueCommands(CommandSource commandSource, string commandText);

        /// <summary>
        /// Insert commands at a given position in the queue
        /// </summary>
        /// <param name="commandSource"></param>
        /// <param name="commandText"></param>
        /// <param name="index">Where to insert the command. Must be larger than or equal to 0 and smaller than or equal to <see cref="QueuedCommandCount"/></param>
        void InsertCommands(CommandSource commandSource, string commandText, int index = 0);

        /// <summary>
        /// Sets an alias to the given command text
        /// </summary>
        /// <param name="aliasName"></param>
        /// <param name="commandText"></param>
        void SetAlias(string aliasName, string commandText);

        void Execute();
    }
}
