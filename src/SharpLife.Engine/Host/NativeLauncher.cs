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

using SharpLife.Engine.Shared.Engines;
using System;

namespace SharpLife.Engine.Host
{
    /// <summary>
    /// Provides an entry point for the native wrapper to launch SharpLife with
    /// </summary>
    public static class NativeLauncher
    {
        /// <summary>
        /// Starts the SharpLife engine
        /// </summary>
        /// <param name="isServer">Whether this is starting as a client or dedicated server</param>
        /// <returns>Engine exit code</returns>
        public static int Start(bool isServer)
        {
            var host = new EngineHost();

            var args = Environment.GetCommandLineArgs();

            host.Start(args, isServer ? HostType.DedicatedServer : HostType.Client);

            return 0;
        }
    }
}
