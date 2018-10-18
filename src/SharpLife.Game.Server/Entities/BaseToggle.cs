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
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using System.Diagnostics;
using System.Numerics;

namespace SharpLife.Game.Server.Entities
{
    /// <summary>
    /// Base class for entities that can have a toggled state
    /// </summary>
    [Networkable(UseBaseType = true)]
    public abstract class BaseToggle : NetworkedEntity
    {
        public delegate void CallWhenMoveDoneFunction();

        public ToggleState m_toggle_state;
        public float m_flActivateFinished;//like attack_finished, but for doors
        public float m_flMoveDistance;// how far a door should slide or rotate
        public float m_flWait;
        public float m_flLip;
        public float m_flTWidth;// for plats
        public float m_flTLength;// for plats

        public Vector3 m_vecPosition1;
        public Vector3 m_vecPosition2;
        public Vector3 m_vecAngle1;
        public Vector3 m_vecAngle2;

        public int m_cTriggersLeft;        // trigger_counter only, # of activations remaining
        public float m_flHeight;
        public ObjectHandle m_hActivator;
        private CallWhenMoveDoneFunction _callWhenMoveDone;
        public Vector3 m_vecFinalDest;
        public Vector3 m_vecFinalAngle;

        public int m_bitsDamageInflict; // DMG_ damage type that the door or tigger does

        /// <summary>
        /// Convenience to get or set the activator
        /// Cache the result to avoid repeated entity lookups
        /// </summary>
        public BaseEntity Activator
        {
            get => Context.EntityList.GetEntity(m_hActivator);
            set => m_hActivator = value?.Handle ?? ObjectHandle.Invalid;
        }

        public override bool KeyValue(string key, string value)
        {
            if (key == "lip")
            {
                m_flLip = KeyValueUtils.ParseFloat(value);
                return true;
            }
            else if (key == "wait")
            {
                m_flWait = KeyValueUtils.ParseFloat(value);
                return true;
            }
            else if (key == "master")
            {
                m_sMaster = value;
                return true;
            }
            else if (key == "distance")
            {
                m_flMoveDistance = KeyValueUtils.ParseFloat(value);
                return true;
            }

            return base.KeyValue(key, value);
        }

        /// <summary>
        /// If this button has a master switch, this is the targetname.
        /// A master switch must be of the multisource type.
        /// If all of the switches in the multisource have been triggered,
        /// then the button will be allowed to operate.
        /// Otherwise, it will be deactivated.
        /// </summary>
        public string m_sMaster;

        public void SetMoveDone(CallWhenMoveDoneFunction function)
        {
            ValidateFunction(function);

            _callWhenMoveDone = function;
        }

        // common member functions
        /// <summary>
        /// calculate Velocity and NextThink to reach vecDest from
        /// Origin traveling at flSpeed
        /// </summary>
        /// <param name="vecDest"></param>
        /// <param name="flSpeed"></param>
        public void LinearMove(in Vector3 vecDest, float flSpeed)
        {
            Debug.Assert(flSpeed != 0, "LinearMove:  no speed is defined!");
            //	Debug.Assert(_callWhenMoveDone != null, "LinearMove: no post-move function defined");

            m_vecFinalDest = vecDest;

            // Already there?
            if (vecDest == Origin)
            {
                LinearMoveDone();
                return;
            }

            // set destdelta to the vector needed to move
            var vecDestDelta = vecDest - Origin;

            // divide vector length by speed to get time to reach dest
            var flTravelTime = vecDestDelta.Length() / flSpeed;

            // set nextthink to trigger a call to LinearMoveDone when dest is reached
            NextThink = LastThinkTime + flTravelTime;
            SetThink(LinearMoveDone);

            // scale the destdelta vector by the time spent traveling to get velocity
            Velocity = vecDestDelta / flTravelTime;
        }

        /// <summary>
        /// After moving, set origin to exact final destination, call "move done" function
        /// </summary>
        public void LinearMoveDone()
        {
            var delta = m_vecFinalDest - Origin;
            var error = delta.Length();
            if (error > 0.03125)
            {
                LinearMove(m_vecFinalDest, 100);
                return;
            }

            Origin = m_vecFinalDest;
            Velocity = Vector3.Zero;
            NextThink = -1;
            _callWhenMoveDone?.Invoke();
        }

        /// <summary>
        /// calculate Velocity and NextThink to reach vecDest from
        /// Origin traveling at flSpeed
        /// Just like LinearMove, but rotational.
        /// </summary>
        /// <param name="vecDestAngle"></param>
        /// <param name="flSpeed"></param>
        public void AngularMove(in Vector3 vecDestAngle, float flSpeed)
        {
            Debug.Assert(flSpeed != 0, "AngularMove:  no speed is defined!");
            //	Debug.Assert(_allWhenMoveDone != null, "AngularMove: no post-move function defined");

            m_vecFinalAngle = vecDestAngle;

            // Already there?
            if (vecDestAngle == Angles)
            {
                AngularMoveDone();
                return;
            }

            // set destdelta to the vector needed to move
            var vecDestDelta = vecDestAngle - Angles;

            // divide by speed to get time to reach dest
            var flTravelTime = vecDestDelta.Length() / flSpeed;

            // set nextthink to trigger a call to AngularMoveDone when dest is reached
            NextThink = LastThinkTime + flTravelTime;
            SetThink(AngularMoveDone);

            // scale the destdelta vector by the time spent traveling to get velocity
            AngularVelocity = vecDestDelta / flTravelTime;
        }

        /// <summary>
        /// After rotating, set angle to exact final angle, call "move done" function
        /// </summary>
        public void AngularMoveDone()
        {
            Angles = m_vecFinalAngle;
            AngularVelocity = Vector3.Zero;
            NextThink = -1;
            _callWhenMoveDone?.Invoke();
        }

        public bool IsLockedByMaster()
        {
            return m_sMaster != null && !EntityUtils.IsMasterTriggered(m_sMaster, Context.EntityList.GetEntity(m_hActivator));
        }
    }
}
