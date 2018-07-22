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

using SharpLife.Engine.API.Game;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.ModUtils;
using System;

namespace SharpLife.Engine.Server.Host
{
    public class EngineServerHost : IEngineServerHost
    {
        private readonly IEngine _engine;

        private ModData<IServerMod> _mod;

        public EngineServerHost(IEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            //Load the game mod assembly
            _mod = ModLoadUtils.LoadMod<IServerMod>(
                _engine.GameDirectory,
                _engine.GameConfiguration.ServerMod.AssemblyName,
                _engine.GameConfiguration.ServerMod.EntrypointClass);
        }
    }
}
