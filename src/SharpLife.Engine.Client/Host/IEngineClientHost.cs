﻿/***
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

using SharpLife.CommandSystem;
using SharpLife.Utility.Events;

namespace SharpLife.Engine.Client.Host
{
    /// <summary>
    /// The client host, responsible for all engine level client operations
    /// </summary>
    public interface IEngineClientHost
    {
        /// <summary>
        /// The command system used by the client
        /// </summary>
        IConCommandSystem CommandSystem { get; }

        /// <summary>
        /// The event system used by the client
        /// </summary>
        IEventSystem EventSystem { get; }

        void PostInitialize();

        void Shutdown();

        void Update(float deltaSeconds);

        void Draw();

        /// <summary>
        /// Connect to a server via address
        /// </summary>
        /// <param name="address"></param>
        void Connect(string address);

        void Disconnect();
    }
}