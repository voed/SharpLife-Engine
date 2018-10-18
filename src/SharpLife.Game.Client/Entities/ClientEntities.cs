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

using SharpLife.Engine.Shared.API.Engine.Client;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Game.Client.Entities.EntityList;
using SharpLife.Game.Client.Renderer.Shared;
using SharpLife.Game.Client.Renderer.Shared.Models;
using SharpLife.Game.Shared;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using SharpLife.Utility;
using System;
using System.Linq;
using System.Reflection;

namespace SharpLife.Game.Client.Entities
{
    public sealed class ClientEntities : IFrameListReceiverListener
    {
        private readonly IClientEngine _clientEngine;

        private readonly ITime _engineTime;

        private readonly IEngineModels _engineModels;

        private IRenderer _renderer;

        private ClientEntityList _entityList;

        private INetworkObjectList _entitiesNetworkList;

        public EntityDictionary EntityDictionary { get; } = new EntityDictionary();

        public EntityContext Context { get; private set; }

        public ClientEntities(IClientEngine clientEngine, ITime engineTime, IEngineModels engineModels)
        {
            _clientEngine = clientEngine ?? throw new ArgumentNullException(nameof(clientEngine));
            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));
            _engineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
        }

        public void Startup(IRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            EntityDictionary.AddTypesFromAssembly<BaseEntity>(typeof(ClientEntities).Assembly);
        }

        public void RegisterNetworkableEntities(TypeRegistryBuilder typeRegistryBuilder)
        {
            //Register all entity classes that have been marked networkable
            //This includes classes not marked with LinkEntityToClass since base types can be mapped to
            foreach (var type in typeof(ClientEntities)
                .Assembly
                .GetTypes()
                .ToList()
                .Where(type => typeof(BaseEntity).IsAssignableFrom(type)
                && type.GetCustomAttribute<NetworkableAttribute>() != null))
            {
                var networkableAttr = type.GetCustomAttribute<NetworkableAttribute>();

                //All entities use this because they don't share a base class
                //Make sure not to replace words in names, just the namespace
                var mapsFromType = networkableAttr.MapsFromType ?? type.FullName.Replace(".Client.", ".Server.");

                typeRegistryBuilder
                    .NewBuilder(type)
                    .AddNetworkedFields()
                    .AddNetworkedProperties()
                    .MapsFromType(mapsFromType)
                    .WithFactory(EntityFactory)
                    .Build();
            }
        }

        public void CreateNetworkObjectLists(INetworkObjectListReceiverBuilder engineObjectLists)
        {
            _entitiesNetworkList = engineObjectLists.CreateList(GameConstants.NetworkObjectLists.EntitiesListName, this);
        }

        public void MapLoadBegin()
        {
            _entityList = new ClientEntityList(EntityDictionary, _clientEngine.MaxClients, this);

            Context = new EntityContext(_clientEngine, _engineTime, _engineModels, _renderer, _entityList);
        }

        public void MapShutdown()
        {
            Context = null;
            _entityList = null;
        }

        //Factory function for networked entities
        public INetworkable EntityFactory(TypeMetaData metaData, in ObjectHandle handle)
        {
            //This can fail if an entity is relying on an abstract base class or a class with no default constructor to provide networking support
            var entity = (BaseEntity)Activator.CreateInstance(metaData.Type);

            entity.Handle = handle;

            return entity;
        }

        public void OnBeginProcessList(INetworkObjectList networkObjectList)
        {
        }

        public void OnEndProcessList(INetworkObjectList networkObjectList)
        {
        }

        public void OnNetworkObjectCreated(INetworkObjectList networkObjectList, INetworkObject networkObject, INetworkable networkableObject)
        {
            //Only entities can be added
            if (!(networkableObject is BaseEntity))
            {
                throw new InvalidOperationException($"Attempted to create a network object of type {networkableObject.GetType().FullName} in the entity list");
            }

            var entity = (BaseEntity)networkableObject;

            entity.NetworkObject = networkObject;

            _entityList.AddEntityToList(entity);
        }

        public void OnNetworkObjectDestroyed(INetworkObjectList networkObjectList, INetworkObject networkObject, INetworkable networkableObject)
        {
            //Only entities can be destroyed
            if (!(networkableObject is BaseEntity))
            {
                throw new InvalidOperationException($"Attempted to destroy a network object of type {networkableObject.GetType().FullName} in the entity list");
            }

            _entityList.DestroyEntity((BaseEntity)networkableObject);
        }

        public void OnBeginUpdateNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject)
        {
            var entity = (BaseEntity)networkObject.Instance;

            entity.OnBeginUpdate();
        }

        public void OnEndUpdateNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject)
        {
            var entity = (BaseEntity)networkObject.Instance;

            entity.OnEndUpdate();
        }

        public void RenderEntities(IModelRenderer modelRenderer, IViewState viewState)
        {
            //This can be called when no maps have been loaded
            if (_entityList != null)
            {
                foreach (var entity in _entityList)
                {
                    if (entity is IRenderableEntity renderable)
                    {
                        renderable.Render(modelRenderer, viewState);
                    }
                }
            }
        }
    }
}
