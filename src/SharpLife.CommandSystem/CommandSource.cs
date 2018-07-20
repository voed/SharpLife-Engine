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
    /// <summary>
    /// The source of a command
    /// Depending on whether commands are executing on the client or server this can affect filtering operations and output redirecting
    /// </summary>
    public enum CommandSource
    {
        /// <summary>
        /// Command was sent by a client
        /// </summary>
        Client = 0,

        /// <summary>
        /// Command was sent locally
        /// </summary>
        Local
    }
}
