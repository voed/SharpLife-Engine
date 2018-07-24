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

using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.Engine.API.Game;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.ModUtils;
using System;

namespace SharpLife.Engine.Server.Host
{
    public class EngineServerHost : IEngineServerHost
    {
        public IConCommandSystem CommandSystem => _engine.CommandSystem;

        public bool GameAssemblyLoaded => _mod != null;

        private readonly IEngine _engine;

        private ModData<IServerMod> _mod;

        public EngineServerHost(IEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public void Shutdown()
        {
            _mod?.Entrypoint.Shutdown();
        }

        public void LoadGameAssembly()
        {
            if (_mod == null)
            {
                //Load the game mod assembly
                _mod = ModLoadUtils.LoadMod<IServerMod>(
                    _engine.GameDirectory,
                    _engine.GameConfiguration.ServerMod.AssemblyName,
                    _engine.GameConfiguration.ServerMod.EntrypointClass);

                var collection = new ServiceCollection();

                var serviceProvider = collection.BuildServiceProvider();

                _mod.Entrypoint.Initialize(serviceProvider);
            }
        }

        public bool Start(string mapName, string startSpot = null, ServerStartFlags flags = ServerStartFlags.None)
        {
            //TODO: report progress to listeners

            //TODO: disconnect clients

            Log.Logger.Information($"Loading map \"{mapName}\"");

            //TODO: print server vars

            //TODO: configure networking

            //TODO: set hostname

            if (startSpot != null)
            {
                Log.Logger.Debug($"Spawn Server {mapName}: [{startSpot}]\n");
            }
            else
            {
                Log.Logger.Debug($"Spawn Server {mapName}\n");
            }

            //TODO: clear custom data if size exceeds maximum

            //TODO: allocate client memory

            if (!_engine.MapManager.LoadMap(mapName))
            {
                Log.Logger.Information($"Couldn't spawn server {_engine.MapManager.FormatMapFileName(mapName)}\n");
                return false;
            }

            //TODO: add world to precache, precache bsp models

            return true;
        }

        public void Stop()
        {
            //TODO: implement
        }
    }
}
