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

using SharpLife.Game.Shared.Entities.EntityList;
using SharpLife.Game.Shared.Entities.MetaData;
using System;

namespace SharpLife.Game.Client.Entities.EntityList
{
    public sealed class ClientEntityList : BaseEntityList<BaseEntity>
    {
        private readonly ClientEntities _entities;

        public ClientEntityList(EntityDictionary entityDictionary, int maxClients, ClientEntities entities)
            : base(entityDictionary, maxClients)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public void AddEntityToList(BaseEntity instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            InternalAddEntityToList(instance.Handle, instance);
        }

        protected override void OnEntityCreated(EntityEntry entry)
        {
            entry.Entity.Context = _entities.Context;
        }

        protected override void OnEntityDestroyed(EntityEntry entry)
        {
        }
    }
}
