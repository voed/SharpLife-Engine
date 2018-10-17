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

namespace SharpLife.Engine.Shared.API.Engine.Server
{
    public interface IServerClients
    {
        /// <summary>
        /// The maximum number of clients that can be connected at the same time
        /// </summary>
        int MaxClients { get; }

        /// <summary>
        /// The number of clients on the server
        /// </summary>
        int Count { get; }
    }
}
