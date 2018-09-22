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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models;

namespace SharpLife.Game.Server.Entities
{
    /// <summary>
    /// Base class for all server entities
    /// </summary>
    [Networkable]
    public abstract class BaseEntity : SharedBaseEntity
    {
        public EntityContext Context { get; set; }

        /// <summary>
        /// Call this if you have a networked member with change notifications enabled
        /// </summary>
        /// <param name="name"></param>
        protected void OnMemberChanged(string name)
        {
            NetworkObject?.OnChange(name);
        }

        /// <summary>
        /// Creates a new entity
        /// </summary>
        /// <param name="networked">Whether the entity is networked</param>
        protected BaseEntity(bool networked)
            : base(networked)
        {
        }

        /// <summary>
        /// Called when a map keyvalue is passed for initialization
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool KeyValue(string key, string value)
        {
            //TODO: rework keyvalues
            if (key == "origin")
            {
                Origin = KeyValueUtils.ParseVector3(value);

                return true;
            }

            if (key == "angles")
            {
                Angles = KeyValueUtils.ParseVector3(value);

                return true;
            }

            if (key == "scale")
            {
                Scale = KeyValueUtils.ParseFloat(value);

                return true;
            }

            if (key == "model")
            {
                Model = Context.EngineModels.LoadModel(value);
                return true;
            }

            if (key == "renderfx")
            {
                RenderFX = KeyValueUtils.ParseEnum(value, RenderFX.None);
                return true;
            }

            if (key == "rendermode")
            {
                RenderMode = KeyValueUtils.ParseEnum(value, RenderMode.Normal);
                return true;
            }

            if (key == "renderamt")
            {
                RenderAmount = KeyValueUtils.ParseInt(value);
                return true;
            }

            if (key == "rendercolor")
            {
                RenderColor = KeyValueUtils.ParseVector3(value);

                return true;
            }

            return false;
        }

        public virtual void Precache()
        {
            //Nothing
        }

        protected virtual void Spawn()
        {
            //Nothing
        }

        /// <summary>
        /// Initializes the global state of this entity
        /// </summary>
        private void InitializeGlobalState()
        {
            //TODO: implement
        }

        /// <summary>
        /// Initializes the entity and makes it ready for use in the world
        /// </summary>
        public void Initialize()
        {
            //TODO: initialize mins and maxs
            //TODO: handle logic from DispatchSpawn
            Spawn();

            if (!PendingDestruction)
            {
                //TODO: Check if i can spawn
            }

            if (!PendingDestruction)
            {
                InitializeGlobalState();
            }
        }

        //TODO: implement proper think logic
        public virtual void Think()
        {
        }
    }
}
