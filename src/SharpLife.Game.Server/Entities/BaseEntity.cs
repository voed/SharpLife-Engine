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

using SharpLife.Game.Server.Physics;
using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models;
using SharpLife.Models;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System;
using System.Numerics;

namespace SharpLife.Game.Server.Entities
{
    /// <summary>
    /// Base class for all server entities
    /// </summary>
    [Networkable]
    public abstract class BaseEntity : SharedBaseEntity
    {
        public EntityContext Context { get; set; }

        public PhysicsState PhysicsState { get; } = new PhysicsState();

        public string TargetName { get; set; }

        public uint SpawnFlags { get; set; }

        private Vector3 _origin;

        /// <summary>
        /// Gets the origin by reference
        /// Avoid using this
        /// </summary>
        public ref Vector3 RefOrigin => ref _origin;

        [Networked]
        public Vector3 Origin
        {
            get => _origin;

            set
            {
                _origin = value;

                Context.Physics.LinkEdict(this, false);
            }
        }

        public Vector3 _absMin;
        public Vector3 _absMax;

        public Vector3 AbsMin
        {
            get => _absMin;
            set => _absMin = value;
        }

        public Vector3 AbsMax
        {
            get => _absMax;
            set => _absMax = value;
        }

        public Vector3 AngularVelocity { get; set; }

        public float Friction { get; set; }

        public float Gravity { get; set; }

        public FixAngleMode FixAngle { get; set; }

        public Vector3 Mins { get; set; }

        public Vector3 Maxs { get; set; }

        public Vector3 Size { get; set; }

        private Vector3 _baseVelocity;

        public ref Vector3 RefBaseVelocity => ref _baseVelocity;

        /// <summary>
        /// Gets the base velocity by reference
        /// Avoid using this
        /// </summary>
        public Vector3 BaseVelocity
        {
            get => _baseVelocity;
            set => _baseVelocity = value;
        }

        private Vector3 _moveDirection;

        /// <summary>
        /// Gets the move direction by reference
        /// Avoid using this
        /// </summary>
        public ref Vector3 RefMoveDirection => ref _moveDirection;

        public Vector3 MoveDirection
        {
            get => _moveDirection;
            set => _moveDirection = value;
        }

        public float Speed { get; set; }

        public Vector3 ViewOffset { get; set; }

        public Vector3 ViewAngle { get; set; }

        public Solid Solid { get; set; }

        public MoveType MoveType { get; set; }

        public ObjectHandle AimEntity { get; set; }

        public ObjectHandle Owner { get; set; }

        public ObjectHandle GroundEntity { get; set; }

        //TODO: figure out if this is needed for all entities
        public Contents Contents { get; set; }

        //TODO: figure out if this is needed for all entities
        public int Buoyancy { get; set; }

        public WaterLevel WaterLevel { get; set; }

        public Contents WaterType { get; set; }

        public float Damage { get; set; }

        public float DamageTime { get; set; }

        public float AirFinished { get; set; }

        public float RadSuitFinished { get; set; }

        public float PainFinished { get; set; }

        public DeadFlag DeadFlag { get; set; }

        public float NextThink { get; set; }

        /// <summary>
        /// TODO: BSP specific
        /// </summary>
        public float LastThinkTime { get; set; }

        /// <summary>
        /// Hack for CS: indicates whether the player has a shield
        /// Used to ignore the shield hitbox in traces
        /// TODO: replace with hitbox filter of some kind
        /// </summary>
        public bool HasShield { get; set; }

        /// <summary>
        /// Indicates whether this entity is a player
        /// </summary>
        public virtual bool IsPlayer => false;

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

        /// <summary>
        /// Sets the size of the entity's bounds
        /// </summary>
        /// <param name="mins"></param>
        /// <param name="maxs"></param>
        public void SetSize(in Vector3 mins, in Vector3 maxs)
        {
            if (mins.X > maxs.X
                || mins.Y > maxs.Y
                || mins.Z > maxs.Z)
            {
                throw new InvalidOperationException("backwards mins/maxs");
            }

            Mins = mins;
            Maxs = maxs;
            Size = maxs - mins;

            Context.Physics.LinkEdict(this, false);
        }

        protected override void OnModelChanged(IModel oldModel, IModel newModel)
        {
            base.OnModelChanged(oldModel, newModel);

            //Set up the size members
            if (newModel != null)
            {
                SetSize(newModel.Mins, newModel.Maxs);
            }
            else
            {
                SetSize(Vector3.Zero, Vector3.Zero);
            }
        }

        public void SetAbsBox()
        {
            //TODO: implement
        }

        public delegate void ThinkFunction();

        public delegate void TouchFunction(BaseEntity other);

        public delegate void BlockedFunction(BaseEntity other);

        private ThinkFunction _thinkFunction;

        private TouchFunction _touchFunction;

        private BlockedFunction _blockedFunction;

        protected void ValidateFunction(Delegate function)
        {
            if (function != null && function.Target != this)
            {
                throw new InvalidOperationException(
                    $"Function {function.Method.DeclaringType.FullName}.{function.Method.Name} is not a part of this class instance {GetType().FullName} ({ClassName})");
            }
        }

        public void SetThink(ThinkFunction function)
        {
            ValidateFunction(function);

            _thinkFunction = function;
        }

        public void SetTouch(TouchFunction function)
        {
            ValidateFunction(function);

            _touchFunction = function;
        }

        public void SetBlocked(BlockedFunction function)
        {
            ValidateFunction(function);

            _blockedFunction = function;
        }

        public virtual void Think()
        {
            _thinkFunction?.Invoke();
        }

        /// <summary>
        /// Called when the engine believes two entities are about to collide.
        /// Return 0 if you want the two entities to just pass through each other without colliding or calling the touch function.
        /// </summary>
        /// <param name="other">Entity being collided with. Can be null</param>
        /// <returns></returns>
        public virtual bool ShouldCollide(BaseEntity other)
        {
            return true;
        }

        public virtual void Touch(BaseEntity other)
        {
            _touchFunction?.Invoke(other);
        }

        public virtual void Blocked(BaseEntity other)
        {
            _blockedFunction?.Invoke(other);
        }
    }
}
