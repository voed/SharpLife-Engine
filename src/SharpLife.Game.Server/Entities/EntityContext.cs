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
using SharpLife.Game.Shared.Entities.EntityList;
using System;

namespace SharpLife.Game.Server.Entities
{
    public sealed class EntityContext
    {
        public IServerEngine ServerEngine { get; }

        public IEngineModels EngineModels { get; }

        public BaseEntityList<BaseEntity> EntityList { get; }

        public EntityContext(IServerEngine serverEngine, IEngineModels engineModels, BaseEntityList<BaseEntity> entityList)
        {
            ServerEngine = serverEngine ?? throw new ArgumentNullException(nameof(serverEngine));
            EngineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
            EntityList = entityList ?? throw new ArgumentNullException(nameof(entityList));
        }
    }
}
