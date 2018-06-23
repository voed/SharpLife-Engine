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

namespace SharpLife.Engine.CommandSystem.Commands
{
    /// <summary>
    /// Contains information about a console command
    /// </summary>
    public sealed class ConCommandInfo : BaseCommandInfo<ConCommandInfo>
    {
        private readonly List<Delegates.ConCommandExecutor> _onExecuteDelegates = new List<Delegates.ConCommandExecutor>();

        public IReadOnlyList<Delegates.ConCommandExecutor> Executors => _onExecuteDelegates;

        /// <summary>
        /// Creates a new info instance
        /// </summary>
        /// <param name="name"></param>
        /// <param name="executor">Must be valid</param>
        public ConCommandInfo(string name, Delegates.ConCommandExecutor executor)
            : base(name)
        {
            _onExecuteDelegates.Add(executor ?? throw new ArgumentNullException(nameof(executor)));
        }

        /// <summary>
        /// Adds another executor
        /// </summary>
        /// <param name="executor">Must be valid</param>
        /// <returns></returns>
        public ConCommandInfo WithCallback(Delegates.ConCommandExecutor executor)
        {
            _onExecuteDelegates.Add(executor ?? throw new ArgumentNullException(nameof(executor)));

            return this;
        }
    }
}
