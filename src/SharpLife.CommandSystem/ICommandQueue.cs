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

namespace SharpLife.CommandSystem
{
    public interface ICommandQueue
    {
        /// <summary>
        /// The number of commands that have been queued up
        /// </summary>
        int QueuedCommandCount { get; }

        /// <summary>
        /// Whether to wait until the next call to <see cref="Execute"/> to continue executing commands
        /// </summary>
        bool Wait { get; set; }

        /// <summary>
        /// Adds commands to the end of the queue
        /// </summary>
        /// <param name="context"></param>
        /// <param name="commandText"></param>
        void QueueCommands(ICommandContext context, string commandText);

        /// <summary>
        /// Insert commands at a given position in the queue
        /// </summary>
        /// <param name="context"></param>
        /// <param name="commandText"></param>
        /// <param name="index">Where to insert the command. Must be larger than or equal to 0 and smaller than or equal to <see cref="QueuedCommandCount"/></param>
        void InsertCommands(ICommandContext context, string commandText, int index = 0);

        void Execute();
    }
}
