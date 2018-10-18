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

using Serilog;
using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Game.Server.API;
using SharpLife.Game.Server.Entities.EntityList;
using SharpLife.Game.Server.Physics;
using SharpLife.Game.Shared;
using SharpLife.Game.Shared.Entities.EntityList;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Maps;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission;
using SharpLife.Utility;
using SharpLife.Utility.Text;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpLife.Game.Server.Entities
{
    public sealed class ServerEntities
    {
        private readonly ILogger _logger;

        private readonly IServerEngine _serverEngine;

        private readonly ITime _engineTime;

        private readonly IEngineModels _serverModels;

        private readonly GameServer _gameServer;

        public ServerEntityList EntityList { get; private set; }

        private INetworkObjectList _entitiesNetworkList;

        public EntityDictionary EntityDictionary { get; } = new EntityDictionary();

        public EntityContext Context { get; private set; }

        /// <summary>
        /// The current world instance
        /// </summary>
        public World World { get; set; }

        public ServerEntities(ILogger logger,
            IServerEngine serverEngine,
            ITime engineTime,
            IEngineModels serverModels,
            GameServer gameServer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serverEngine = serverEngine ?? throw new ArgumentNullException(nameof(serverEngine));
            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));
            _serverModels = serverModels ?? throw new ArgumentNullException(nameof(serverModels));
            _gameServer = gameServer ?? throw new ArgumentNullException(nameof(gameServer));
        }

        public void Startup()
        {
            EntityDictionary.AddTypesFromAssembly<BaseEntity>(typeof(ServerEntities).Assembly);
        }

        public void RegisterNetworkableEntities(TypeRegistryBuilder typeRegistryBuilder)
        {
            var registeredTypes = new HashSet<Type>();

            void AddType(Type type, NetworkableAttribute networkableAttr)
            {
                //MapsFromType is not required on the server side

                typeRegistryBuilder
                    .NewBuilder(type)
                    .AddNetworkedFields()
                    .AddNetworkedProperties()
                    .Build();

                registeredTypes.Add(type);
            }

            bool AddBaseType(Type type)
            {
                if (type != null)
                {
                    //Already registered, we're done
                    if (registeredTypes.Contains(type))
                    {
                        return true;
                    }

                    var networkableAttr = type.GetCustomAttribute<NetworkableAttribute>();

                    if (networkableAttr != null)
                    {
                        if (!networkableAttr.UseBaseType)
                        {
                            AddType(type, networkableAttr);
                            return true;
                        }

                        //Base class also depends on its base, try that
                        //This will recursively register all base types
                        var registered = AddBaseType(type.BaseType);

                        if (!registered)
                        {
                            throw new NotSupportedException($"The type {type.FullName} is declared as using a base class for networking, but no base class is configured for networking");
                        }

                        typeRegistryBuilder.RegisterRemappedType(type, type.BaseType);

                        //Register the type as networked so any subclasses will know
                        registeredTypes.Add(type);

                        return registered;
                    }
                }

                return false;
            }

            foreach (var entityMetaData in EntityDictionary.Types)
            {
                //Don't need to check return value because entities can be server only
                AddBaseType(entityMetaData.Type);
            }
        }

        public void CreateNetworkObjectLists(INetworkObjectListTransmitterBuilder networkObjectListBuilder)
        {
            _entitiesNetworkList = networkObjectListBuilder.CreateList(GameConstants.NetworkObjectLists.EntitiesListName);
        }

        public void CreateEntityList()
        {
            EntityList = new ServerEntityList(
                EntityDictionary,
                _serverEngine.Clients.MaxClients,
                _entitiesNetworkList,
                this);
        }

        public void MapLoadBegin(ITime gameTime, IMapInfo mapInfo, GamePhysics gamePhysics, string entityData, bool loadGame)
        {
            //TODO: the game needs a different time object that tracks game time
            Context = new EntityContext(_serverEngine, gameTime, _serverModels, mapInfo, _gameServer, this, gamePhysics, EntityList);

            if (loadGame)
            {
                //TODO: load game
            }
            else
            {
                LoadEntities(entityData);
            }

            _gameServer.GameBridge.DataReceiver?.ReceiveEntityList(EntityList);
        }

        public void Deactivate()
        {
            _gameServer.GameBridge.DataReceiver?.ReceiveEntityList(null);
            Context = null;
            EntityList = null;
        }

        public void StartFrame()
        {
            //Nothing
        }

        private void LoadEntities(string entityData)
        {
            var keyvalues = KeyValuesParser.ParseAll(entityData);

            for (var index = 0; index < keyvalues.Count; ++index)
            {
                //Better error handling than the engine: if an entity couldn't be created, log it and keep going
                try
                {
                    LoadEntity(keyvalues[index], index);
                }
                catch (EntityInstantiationException e)
                {
                    _logger.Error(e, $"A problem occurred while creating entity {index}");
                }
            }
        }

        private string GetClassName(List<KeyValuePair<string, string>> block, int index)
        {
            var name = block.Find(kv => kv.Key == "classname");

            if (name.Key == null)
            {
                //The engine only handles this error if there is a classname key that the game doesn't handle
                throw new EntityInstantiationException($"No classname for entity {index}");
            }

            if (string.IsNullOrWhiteSpace(name.Value))
            {
                throw new EntityInstantiationException($"Classname for entity {index} is invalid");
            }

            return name.Value;
        }

        private void LoadEntity(List<KeyValuePair<string, string>> block, int index)
        {
            var className = GetClassName(block, index);

            var metaData = EntityDictionary.FindEntityMetaData(className);

            if (metaData == null)
            {
                throw new NoSuchEntityClassException(className);
            }

            BaseEntity entity = null;

            //The world always has index 0
            if (index == 0)
            {
                entity = EntityList.CreateEntity(metaData, 0);
            }
            else
            {
                entity = EntityList.CreateEntity(metaData);
            }

            var initialized = false;

            try
            {
                foreach (var kv in block)
                {
                    var key = kv.Key;
                    var value = kv.Value;

                    //Don't do this multiple times
                    if (key != "classname")
                    {
                        //The engine does not allow values with the same content as the classname to be passed
                        //No reason to impose this restriction here

                        CheckKeyValue(entity, ref key, ref value);

                        //TODO: implement
                        //if (!KeyValueUtils.TrySetKeyValue(entity, metaData, key, value))
                        {
                            entity.KeyValue(key, value);
                        }
                    }
                }

                _logger.Information($"Spawning entity {entity.ClassName} ({entity.GetType().FullName})");
                entity.Initialize();

                initialized = true;
                //TODO: can check if the entity is a template and do stuff here
            }
            finally
            {
                //On failure always free the entity
                if (!initialized || entity.PendingDestruction)
                {
                    EntityList.DestroyEntity(entity);
                }
            }
        }

        /// <summary>
        /// Checks a keyvalue for anything that needs to be converted
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void CheckKeyValue(BaseEntity entity, ref string key, ref string value)
        {
            // anglehack is to allow QuakeEd to write single scalar angles
            // and allow them to be turned into vectors. (FIXME...)
            if (key == "angle")
            {
                float.TryParse(value, out var floatValue);

                if (floatValue >= 0)
                {
                    value = $"{entity.Angles.X} {floatValue} {entity.Angles.Z}";
                }
                else
                {
                    if (Math.Floor(floatValue) == -1)
                    {
                        value = "-90 0 0";
                    }
                    else
                    {
                        value = "90 0 0";
                    }
                }

                key = "angles";
            }
        }
    }
}
