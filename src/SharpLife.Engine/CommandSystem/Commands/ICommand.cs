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


using System.Collections.Generic;

namespace SharpLife.Engine.CommandSystem.Commands
{
    public interface ICommand : IEnumerable<string>
    {
        /// <summary>
        /// Where this command came from
        /// </summary>
        CommandSource CommandSource { get; }

        /// <summary>
        /// Gets the name of the command to execute
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the number of arguments in this command
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets arguments by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string this[int index] { get; }

        /// <summary>
        /// Gets a copy of the arguments as a list
        /// </summary>
        IList<string> Arguments { get; }

        /// <summary>
        /// Gets the command as a string
        /// </summary>
        string CommandString { get; }

        /// <summary>
        /// Gets the arguments as a string
        /// </summary>
        string ArgumentsString { get; }

        /// <summary>
        /// Gets the arguments as a starting, taking all arguments starting with <paramref name="firstArgumentIndex"/>
        /// </summary>
        /// <param name="firstArgumentIndex"></param>
        /// <returns></returns>
        string ArgumentsAsString(int firstArgumentIndex);
    }
}
