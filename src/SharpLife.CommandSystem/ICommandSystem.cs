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
    public interface ICommandSystem
    {
        ICommandQueue Queue { get; }

        /// <summary>
        /// The shared context
        /// All commands added to this context will be shared between all contexts
        /// Shared commands will only be able to execute commands in the context that they were executed in
        /// </summary>
        ICommandContext SharedContext { get; }

        ICommandContext CreateContext(string name, object tag = null);

        void DestroyContext(ICommandContext context);

        void Execute();
    }
}
