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
    /// <summary>
    /// String message identifiers used by networking code
    /// </summary>
    public static class NetMessages
    {
        public const string ClientDisconnectMessage = "dropclient";

        public const string ServerShutdownUnknown = "Server shutting down for unknown reason";

        public const string ServerShutdownMessage = "Server shutting down";

        public const string ServerChangeLevel = "Server changing level";

        public const string ServerClientDeniedNoFreeSlots = "Server is full.";
    }
}
