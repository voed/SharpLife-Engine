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
using SharpLife.Game.Client.Renderer.Shared;
using SharpLife.Game.Shared.Entities.EntityList;
using SharpLife.Utility;
using System;

namespace SharpLife.Game.Client.Entities
{
    public sealed class EntityContext
    {
        public IClientEngine ClientEngine { get; }

        public ITime Time { get; }

        public IEngineModels EngineModels { get; }

        public IRenderer Renderer { get; }

        //TODO: should use the same generator used by the original engine
        public Random Random { get; }

        public BaseEntityList<BaseEntity> EntityList { get; }

        public EntityContext(IClientEngine clientEngine, ITime time, IEngineModels engineModels, IRenderer renderer, BaseEntityList<BaseEntity> entityList)
        {
            ClientEngine = clientEngine ?? throw new ArgumentNullException(nameof(clientEngine));
            Time = time ?? throw new ArgumentNullException(nameof(time));
            EngineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            Random = new Random();
            EntityList = entityList ?? throw new ArgumentNullException(nameof(entityList));
        }
    }
}
