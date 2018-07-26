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

using System.Net;

namespace SharpLife.Engine.Client.Networking
{
    /// <summary>
    /// Network data specific to a single server
    /// </summary>
    internal sealed class ServerData
    {
        /// <summary>
        /// Name of the server being connected to
        /// May contain a port value
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Our IP address as reported by the server
        /// </summary>
        public IPEndPoint TrueAddress { get; set; } = new IPEndPoint(IPAddress.None, 0);
    }
}
