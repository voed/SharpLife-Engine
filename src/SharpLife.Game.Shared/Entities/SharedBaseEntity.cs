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

using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.UI.EditableMemberTypes;
using SharpLife.Models;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System.Numerics;

namespace SharpLife.Game.Shared.Entities
{
    /// <summary>
    /// Base class shared between client and server
    /// </summary>
    [Networkable]
    [ObjectEditorVisible(Visible = true)]
    public abstract class SharedBaseEntity : IEntity
    {
        public ObjectHandle Handle { get; set; }

        /// <summary>
        /// If this is a networked object, this is the network object for it
        /// <see cref="Networked"/>
        /// </summary>
        public INetworkObject NetworkObject { get; set; }

        public bool Networked { get; }

        [Networked]
        public string ClassName { get; set; }

        [Networked]
        [ObjectEditorVector3(DisplayFormat = Vector3DisplayFormat.AnglesDegrees)]
        public Vector3 Angles { get; set; }

        private Vector3 _velocity;

        /// <summary>
        /// Gets the velocity by reference
        /// Avoid using this
        /// </summary>
        public ref Vector3 RefVelocity => ref _velocity;

        [Networked]
        public Vector3 Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        [Networked]
        public float Scale { get; set; }

        [Networked]
        public RenderFX RenderFX { get; set; }

        [Networked]
        public RenderMode RenderMode { get; set; }

        [Networked]
        public int RenderAmount { get; set; }

        [Networked]
        [ObjectEditorVector3(DisplayFormat = Vector3DisplayFormat.Color24)]
        public Vector3 RenderColor { get; set; }

        [Networked]
        public EntityFlags Flags { get; set; }

        [Networked]
        public EffectsFlags Effects { get; set; }

        [Networked]
        public float Health { get; set; }

        private IModel _model;

        //TODO: will need handlers to update size
        [Networked]
        public IModel Model
        {
            get => _model;

            set
            {
                var oldModel = _model;

                _model = value;

                OnModelChanged(oldModel, _model);
            }
        }

        /// <summary>
        /// Convenience for checking and setting if an entity has been marked as needing destruction
        /// </summary>
        [ObjectEditorVisible(Visible = false)]
        public bool PendingDestruction
        {
            get => (Flags & EntityFlags.PendingDestruction) != 0;
            set
            {
                if (PendingDestruction)
                {
                    Flags |= EntityFlags.PendingDestruction;
                }
                else
                {
                    Flags &= ~EntityFlags.PendingDestruction;
                }
            }
        }

        /// <summary>
        /// Creates a new entity
        /// </summary>
        /// <param name="networked">Whether the entity is networked</param>
        protected SharedBaseEntity(bool networked)
        {
            Networked = networked;
        }

        public virtual void OnConstruct()
        {
            //Nothing
        }

        public virtual void OnDestruct()
        {
            //Nothing
        }

        /// <summary>
        /// Called whenever the model changes
        /// </summary>
        /// <param name="oldModel"></param>
        /// <param name="newModel"></param>
        protected virtual void OnModelChanged(IModel oldModel, IModel newModel)
        {
        }
    }
}
