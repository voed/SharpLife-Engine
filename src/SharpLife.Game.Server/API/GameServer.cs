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
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Engine.Shared.API.Game.Server;
using SharpLife.Engine.Shared.API.Game.Shared;
using SharpLife.Engine.Shared.Events;
using SharpLife.Game.Server.Entities;
using SharpLife.Game.Server.Networking;
using SharpLife.Game.Shared.Bridge;
using SharpLife.Game.Shared.Maps;
using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Models;
using SharpLife.Models.BSP.FileFormat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SharpLife.Game.Server.API
{
    public class GameServer : IGameServer
    {
        private ILogger _logger;

        private IServerEngine _engine;

        private IServerModels _engineModels;

        private ServerNetworking _networking;

        private ServerEntities _entities;

        private bool _active;

        /// <summary>
        /// Gets the current map info instance
        /// Don't cache this, it gets recreated every map
        /// </summary>
        public IMapInfo MapInfo { get; private set; }

        public IGameBridge GameBridge { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(this);

            //Expose as both to get the implementation
            serviceCollection.AddSingleton<ServerNetworking>();
            serviceCollection.AddSingleton<IServerNetworking>(provider => provider.GetRequiredService<ServerNetworking>());
            serviceCollection.AddSingleton<ServerEntities>();
        }

        public void Startup(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _logger = serviceProvider.GetRequiredService<ILogger>();

            _engine = serviceProvider.GetRequiredService<IServerEngine>();

            _engineModels = serviceProvider.GetRequiredService<IServerModels>();

            if (_engine.IsDedicatedServer)
            {
                GameBridge = Shared.Bridge.GameBridge.CreateBridge(null);
            }
            else
            {
                GameBridge = (GameBridge)serviceProvider.GetRequiredService<IBridge>();
            }

            _networking = serviceProvider.GetRequiredService<ServerNetworking>();

            _entities = serviceProvider.GetRequiredService<ServerEntities>();

            _entities.Startup();
        }

        public void Shutdown()
        {
        }

        public IReadOnlyList<IModelLoader> GetModelLoaders() => GameModelUtils.GetModelLoaders();

        /// <summary>
        /// Returns whether the given map name is valid
        /// </summary>
        /// <param name="mapName">The map name without directory or extension</param>
        /// <returns></returns>
        public bool IsMapValid(string mapName)
        {
            return _engine.FileSystem.Exists(GameBridge.ModelUtils.FormatMapFileName(mapName));
        }

        public bool TryMapLoadBegin(string mapName, ServerStartFlags flags)
        {
            _engine.EventSystem.DispatchEvent(new MapStartedLoading(
                mapName,
                MapInfo?.Name,
                (flags & ServerStartFlags.ChangeLevel) != 0,
                (flags & ServerStartFlags.LoadGame) != 0));

            var mapFileName = GameBridge.ModelUtils.FormatMapFileName(mapName);

            IModel worldModel;

            try
            {
                worldModel = _engineModels.LoadModel(mapFileName);
            }
            catch (Exception e)
            {
                //TODO: needs a rework
                if (e is InvalidOperationException
                    || e is InvalidBSPVersionException
                    || e is IOException)
                {
                    worldModel = null;
                }
                else
                {
                    throw;
                }
            }

            if (worldModel == null)
            {
                _logger.Information($"Couldn't spawn server {mapFileName}");
                return false;
            }

            _engine.EventSystem.DispatchEvent(EngineEvents.ServerMapDataFinishLoad);

            if (!(worldModel is BSPModel bspWorldModel))
            {
                _logger.Information($"Model {mapFileName} is not a map");
                return false;
            }

            _engine.EventSystem.DispatchEvent(EngineEvents.ServerMapCRCComputed);

            MapInfo = new MapInfo(mapName, MapInfo?.Name, bspWorldModel);

            //Load world sub models
            foreach (var i in Enumerable.Range(1, bspWorldModel.BSPFile.Models.Count - 1))
            {
                _engineModels.LoadModel($"{Framework.BSPModelNamePrefix}{i}");
            }

            //Load the fallback model now to ensure that BSP indices are matched up
            _engineModels.LoadFallbackModel();

            //TODO: initialize sky

            return true;
        }

        public void MapLoadContinue(bool loadGame)
        {
            _entities.MapLoadBegin(MapInfo, MapInfo.Model.BSPFile.Entities, loadGame);
        }

        public void MapLoadFinished()
        {

        }

        public void Activate()
        {
            Debug.Assert(!_active);

            _active = true;
        }

        public void Deactivate()
        {
            if (!_active)
            {
                return;
            }

            _active = false;

            _entities.Deactivate();
        }

        public void StartFrame()
        {
            _entities.StartFrame();
        }

        public void EndFrame()
        {

        }
    }
}
