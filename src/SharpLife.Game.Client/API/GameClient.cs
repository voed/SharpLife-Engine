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
using SharpLife.Engine.Shared.API.Engine.Client;
using SharpLife.Engine.Shared.API.Game.Client;
using SharpLife.Engine.Shared.API.Game.Shared;
using SharpLife.Game.Client.Entities;
using SharpLife.Game.Client.Networking;
using SharpLife.Game.Client.Renderer;
using SharpLife.Game.Client.Renderer.Shared;
using SharpLife.Game.Client.Renderer.Shared.Models;
using SharpLife.Game.Client.UI;
using SharpLife.Game.Shared.Bridge;
using SharpLife.Game.Shared.Maps;
using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Models;
using SharpLife.Networking.Shared;
using System;
using System.Collections.Generic;

namespace SharpLife.Game.Client.API
{
    public sealed class GameClient : IGameClient, IRendererListener
    {
        private ILogger _logger;

        private IClientEngine _engine;
        private IGameBridge _gameBridge;

        private Renderer.Renderer _renderer;

        private IClientUI _clientUI;

        private ClientNetworking _networking;

        private ClientEntities _entities;

        /// <summary>
        /// Gets the current map info instance
        /// Don't cache this, it gets recreated every map
        /// Null if not running any map
        /// </summary>
        public IMapInfo MapInfo { get; private set; }

        public BridgeDataReceiver BridgeDataReceiver { get; } = new BridgeDataReceiver();

        public string CachedMapName { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            _gameBridge = GameBridge.CreateBridge(BridgeDataReceiver);

            serviceCollection.AddSingleton<IBridge>(_gameBridge);

            serviceCollection.AddSingleton(this);

            //Expose as both to get the implementation
            serviceCollection.AddSingleton<ClientNetworking>();
            serviceCollection.AddSingleton<IClientNetworking>(provider => provider.GetRequiredService<ClientNetworking>());
            serviceCollection.AddSingleton<ClientEntities>();
        }

        public void Startup(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _logger = serviceProvider.GetRequiredService<ILogger>();

            _engine = serviceProvider.GetRequiredService<IClientEngine>();

            _networking = serviceProvider.GetRequiredService<ClientNetworking>();

            _entities = serviceProvider.GetRequiredService<ClientEntities>();

            _clientUI = new ImGuiInterface(_logger, _engine, this);

            _renderer = new Renderer.Renderer(
                _engine.GameWindow,
                _logger,
                _engine.FileSystem,
                _engine.CommandContext,
                _engine.UserInterface.WindowManager.InputSystem,
                _engine.Time,
                this,
                Framework.Path.EnvironmentMaps,
                Framework.Path.Shaders);

            _engine.GameWindow.Resized += _renderer.WindowResized;

            _entities.Startup(_renderer);
        }

        public void Shutdown()
        {
        }

        public IReadOnlyList<IModelLoader> GetModelLoaders() => GameModelUtils.GetModelLoaders();

        public void MapLoadBegin()
        {
            var mapName = CachedMapName;

            var worldModel = _engine.Models.LoadModel(mapName);

            //This should never happen since the map file is compared by CRC before being loaded
            //TODO: use proper exception type that engine can catch
            if (!(worldModel is BSPModel bspWorldModel))
            {
                throw new InvalidOperationException($"Model {mapName} is not a map");
            }

            MapInfo = new MapInfo(NetUtilities.ConvertToPlatformPath(mapName), MapInfo?.Name, bspWorldModel);

            _clientUI.MapLoadBegin();

            _renderer.LoadModels(MapInfo.Model, _engine.ModelManager);

            _entities.MapLoadBegin();
        }

        public void MapLoadFinished()
        {

        }

        public void MapShutdown()
        {
            _entities.MapShutdown();

            _renderer.ClearBSP();

            MapInfo = null;
        }

        public void Update(float deltaSeconds)
        {
            _renderer.Update(deltaSeconds);

            _clientUI.Update(deltaSeconds, _renderer.Scene);
        }

        public void Draw()
        {
            _clientUI.Draw(_renderer.Scene);

            _renderer.Draw();
        }

        public void OnRenderModels(IModelRenderer modelRenderer, IViewState viewState)
        {
            _entities.RenderEntities(modelRenderer, viewState);
        }
    }
}
