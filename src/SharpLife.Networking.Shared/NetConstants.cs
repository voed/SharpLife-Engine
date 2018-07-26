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

namespace SharpLife.Networking.Shared
{
    public static class NetConstants
    {
        /// <summary>
        /// The App identifier token used for networking
        /// </summary>
        public static readonly string AppIdentifier = "SharpLife";

        /// <summary>
        /// Connect to this computer's local server, if running
        /// </summary>
        public static readonly string LocalHost = "localhost";

        public const int DefaultServerPort = 27015;

        public const int DefaultClientPort = 27005;

        /// <summary>
        /// GoldSource will attempt to retry connecting to a server up to 3 times before aborting
        /// </summary>
        public const int MaxRetries = 3;

        public const int MaxHandshakeAttempts = MaxRetries + 1;

        /// <summary>
        /// Used to determine if the network protocol used by the client and server are compatible
        /// Allows connections to be rejected trivially during approval
        /// </summary>
        public const int ProtocolVersion = 1;

        /// <summary>
        /// The minimum number of clients that can be connected to a server
        /// </summary>
        public const int MinClients = 1;

        /// <summary>
        /// Maximum number of clients that can be connected to a server
        /// </summary>
        public const int MaxClients = 32;
    }
}
