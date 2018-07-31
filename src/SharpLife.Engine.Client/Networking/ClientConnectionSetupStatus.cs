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

namespace SharpLife.Engine.Client.Networking
{
    public enum ClientConnectionSetupStatus
    {
        /// <summary>
        /// Not connected to any server
        /// </summary>
        NotConnected = 0,

        /// <summary>
        /// In the process of connecting to a server
        /// </summary>
        Connecting,

        /// <summary>
        /// Connected to a server
        /// </summary>
        Connected,

        /// <summary>
        /// Initializing the client with server data
        /// </summary>
        Initializing,

        /// <summary>
        /// Connected, initialized and active on the server
        /// </summary>
        Active,
    }
}
