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

namespace SharpLife.CommandSystem.Commands
{
    public static class Delegates
    {
        /// <summary>
        /// Filters commands when executing them
        /// </summary>
        /// <param name="commandToExecute"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public delegate bool CommandFilter(IBaseConsoleCommand commandToExecute, ICommand command);

        /// <summary>
        /// Console command execution delegate
        /// </summary>
        /// <param name="command"></param>
        public delegate void ConCommandExecutor(ICommand command);

        /// <summary>
        /// Console variable change handler
        /// </summary>
        /// <param name="changeEvent">Contains information about which variable changed and what the old value was</param>
        public delegate void ConVarChangeHandler(ref ConVarChangeEvent changeEvent);
    }
}
