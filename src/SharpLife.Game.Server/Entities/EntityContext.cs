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

using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Game.Server.API;
using SharpLife.Game.Server.Physics;
using SharpLife.Game.Shared.Entities.EntityList;
using SharpLife.Game.Shared.Maps;
using SharpLife.Utility;
using System;

namespace SharpLife.Game.Server.Entities
{
    public sealed class EntityContext
    {
        public IServerEngine ServerEngine { get; }

        public ITime Time { get; }

        public IEngineModels EngineModels { get; }

        public IMapInfo MapInfo { get; }

        public GameServer Server { get; }

        public ServerEntities Entities { get; }

        public GamePhysics Physics { get; }

        public BaseEntityList<BaseEntity> EntityList { get; }

        public EntityContext(
            IServerEngine serverEngine,
            ITime time,
            IEngineModels engineModels,
            IMapInfo mapInfo,
            GameServer gameServer,
            ServerEntities entities,
            GamePhysics gamePhysics,
            BaseEntityList<BaseEntity> entityList)
        {
            ServerEngine = serverEngine ?? throw new ArgumentNullException(nameof(serverEngine));
            Time = time ?? throw new ArgumentNullException(nameof(time));
            EngineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
            MapInfo = mapInfo ?? throw new ArgumentNullException(nameof(mapInfo));
            Server = gameServer ?? throw new ArgumentNullException(nameof(gameServer));
            Entities = entities ?? throw new ArgumentNullException(nameof(entities));
            Physics = gamePhysics ?? throw new ArgumentNullException(nameof(gamePhysics));
            EntityList = entityList ?? throw new ArgumentNullException(nameof(entityList));
        }
    }
}
