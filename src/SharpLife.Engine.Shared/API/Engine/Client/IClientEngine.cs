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

using SharpLife.CommandSystem;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Engine.Shared.Logging;
using SharpLife.Engine.Shared.UI;
using SharpLife.FileSystem;
using SharpLife.Models;
using SharpLife.Utility;

namespace SharpLife.Engine.Shared.API.Engine.Client
{
    public interface IClientEngine
    {
        IFileSystem FileSystem { get; }

        ICommandContext CommandContext { get; }

        ILogListener LogListener { get; set; }

        IWindow GameWindow { get; }

        IUserInterface UserInterface { get; }

        ITime Time { get; }

        //TODO: maybe let users access this through IEngineModels
        IModelManager ModelManager { get; }

        IEngineModels Models { get; }
    }
}
