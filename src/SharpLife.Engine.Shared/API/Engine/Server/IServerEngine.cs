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
using SharpLife.FileSystem;
using SharpLife.Utility;
using SharpLife.Utility.Events;

namespace SharpLife.Engine.Shared.API.Engine.Server
{
    public interface IServerEngine
    {
        IFileSystem FileSystem { get; }

        ICommandContext CommandContext { get; }

        ITime EngineTime { get; }

        IEventSystem EventSystem { get; }

        IServerClients Clients { get; }

        bool IsDedicatedServer { get; }
    }
}
