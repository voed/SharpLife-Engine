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
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Utility.Mathematics;
using System;
using System.Diagnostics;

namespace SharpLife.Game.Server.Entities.Doors
{
    /// <summary>
    /// QUAKED func_door (0 .5 .8) ? START_OPEN x DOOR_DONT_LINK TOGGLE
    /// if two doors touch, they are assumed to be connected and operate as a unit.
    /// 
    /// TOGGLE causes the door to wait in both the start and end states for a trigger event.
    /// 
    /// START_OPEN causes the door to move to its destination when spawned, and operate in reverse.
    /// It is used to temporarily or permanently close off an area when triggered (not usefull for
    /// touch or takedamage doors).
    /// 
    /// "angle"         determines the opening direction
    /// "targetname"	if set, no touch field will be spawned and a remote button or trigger
    /// 
    ///                 field activates the door.
    /// "health"        if set, door must be shot open
    /// "speed"         movement speed (100 default)
    /// "wait"          wait before returning(3 default, -1 = never return)
    /// "lip"           lip remaining at end of move(8 default)
    /// "dmg"           damage to inflict when blocked(2 default)
    /// "sounds"
    /// 0)      no sound
    /// 1)      stone
    /// 2)      base
    /// 3)      stone chain
    /// 4)      screechy metal
    /// 
    /// TODO: also doubles as func_water
    /// </summary>
    [LinkEntityToClass("func_door")]
    [Networkable(UseBaseType = true)]
    public class BaseDoor : BaseToggle
    {
        public static class SF
        {
            public const uint DoorStartsOpen = 1 << 0;
            public const uint DoorPassable = 1 << 3;
            public const uint DoorOneWay = 1 << 4;
            public const uint DoorNoAutoReturn = 1 << 5;

            /// <summary>
            /// door must be opened by player's use button
            /// </summary>
            public const uint DoorUseOnly = 1 << 9;
            public const uint DoorSilent = 1U << 31;
        }

        public override bool KeyValue(string key, string value)
        {
            if (key == "skin")
            {
                int.TryParse(value, out var result);
                Contents = (Contents)result;
                return true;
            }

            //TODO: implement

            return base.KeyValue(key, value);
        }

        public override void Precache()
        {
            //TODO: implement
            base.Precache();
        }

        protected override void Spawn()
        {
            Precache();
            EntityUtils.SetMoveDir(this);

            if (Contents == Contents.Node)
            {
                //normal door
                if ((SpawnFlags & SF.DoorPassable) != 0)
                {
                    Solid = Solid.Not;
                }
                else
                {
                    Solid = Solid.BSP;
                }
            }
            else
            {
                // special contents
                Solid = Solid.Not;
                SpawnFlags |= SF.DoorSilent; // water is silent for now
            }

            MoveType = MoveType.Push;
            //TODO:
            //UTIL_SetOrigin(pev, pev->origin);
            //SET_MODEL(ENT(pev), STRING(pev->model));

            if (Speed == 0)
            {
                Speed = 100;
            }

            m_vecPosition1 = Origin;
            // Subtract 2 from size because the engine expands bboxes by 1 in all directions making the size too big
            m_vecPosition2 = m_vecPosition1 + (MoveDirection
                * (Math.Abs(MoveDirection.X * (Size.X - 2))
                + Math.Abs(MoveDirection.Y * (Size.Y - 2))
                + Math.Abs(MoveDirection.Z * (Size.Z - 2))
                - m_flLip));

            Debug.Assert(m_vecPosition1 != m_vecPosition2, "door start/end positions are equal");
            if ((SpawnFlags & SF.DoorStartsOpen) != 0)
            {
                // swap pos1 and pos2, put door at pos2
                Origin = m_vecPosition2;
                m_vecPosition2 = m_vecPosition1;
                m_vecPosition1 = Origin;
            }

            m_toggle_state = ToggleState.AtBottom;

            // if the door is flagged for USE button activation only, use NULL touch function
            if ((SpawnFlags & SF.DoorUseOnly) != 0)
            {
                SetTouch(null);
            }
            else // touchable button
            {
                SetTouch(DoorTouch);
            }
        }

        public override void Blocked(BaseEntity other)
        {
            // Hurt the blocker a little.
            if (Damage != 0)
            {
                //TODO
                //pOther->TakeDamage(pev, pev, pev->dmg, DMG_CRUSH);
            }

            // if a door has a negative wait, it would never come back if blocked,
            // so let it just squash the object to death real fast

            if (m_flWait >= 0)
            {
                if (m_toggle_state == ToggleState.GoingDown)
                {
                    DoorGoUp();
                }
                else
                {
                    DoorGoDown();
                }
            }

            // Block all door pieces with the same targetname here.
            if (!string.IsNullOrEmpty(TargetName))
            {
                //TODO
                /*
                 edict_t* pentTarget = NULL;
                BaseDoor pDoor = null;
                for (; ; )
                {
                    pentTarget = FIND_ENTITY_BY_TARGETNAME(pentTarget, STRING(pev->targetname));

                    if (VARS(pentTarget) != pev)
                    {
                        if (FNullEnt(pentTarget))
                            break;

                        if (FClassnameIs(pentTarget, "func_door") || FClassnameIs(pentTarget, "func_door_rotating"))
                        {

                            pDoor = GetClassPtr((CBaseDoor*)VARS(pentTarget));

                            if (pDoor->m_flWait >= 0)
                            {
                                if (pDoor->pev->velocity == pev->velocity && pDoor->pev->avelocity == pev->velocity)
                                {
                                    // this is the most hacked, evil, bastardized thing I've ever seen. kjb
                                    if (FClassnameIs(pentTarget, "func_door"))
                                    {// set origin to realign normal doors
                                        pDoor->pev->origin = pev->origin;
                                        pDoor->pev->velocity = g_vecZero;// stop!
                                    }
                                    else
                                    {// set angles to realign rotating doors
                                        pDoor->pev->angles = pev->angles;
                                        pDoor->pev->avelocity = g_vecZero;
                                    }
                                }

                                if (!FBitSet(pev->spawnflags, SF_DOOR_SILENT))
                                    STOP_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseMoving));

                                if (pDoor->m_toggle_state == TS_GOING_DOWN)
                                    pDoor->DoorGoUp();
                                else
                                    pDoor->DoorGoDown();
                            }
                        }
                    }
                }
                */
            }
        }

        /// <summary>
        /// Doors not tied to anything (e.g. button, another door) can be touched, to make them activate
        /// </summary>
        /// <param name="pOther"></param>
        private void DoorTouch(BaseEntity pOther)
        {
            // Ignore touches by anything but players
            if (!pOther.IsPlayer)
            {
                return;
            }

            // If door has master, and it's not ready to trigger, 
            // play 'locked' sound

            if (m_sMaster != null && !EntityUtils.IsMasterTriggered(m_sMaster, pOther))
            {
                //TODO
                //PlayLockSounds(pev, &m_ls, TRUE, FALSE);
            }

            // If door is somebody's target, then touching does nothing.
            // You have to activate the owner (e.g. button).

            if (!string.IsNullOrEmpty(TargetName))
            {
                // play locked sound
                //TODO
                //PlayLockSounds(pev, &m_ls, TRUE, FALSE);
                return;
            }

            m_hActivator = pOther.Handle;// remember who activated the door

            if (DoorActivate())
            {
                SetTouch(null); // Temporarily disable the touch function, until movement is finished.
            }
        }

        /// <summary>
        /// Causes the door to "do its thing", i.e. start moving, and cascade activation.
        /// </summary>
        /// <returns></returns>
        private bool DoorActivate()
        {
            var activator = Activator;

            if (!EntityUtils.IsMasterTriggered(m_sMaster, activator))
            {
                return false;
            }

            if ((SpawnFlags & SF.DoorNoAutoReturn) != 0 && m_toggle_state == ToggleState.AtTop)
            {
                // door should close
                DoorGoDown();
            }
            else
            {
                // door should open
                if (activator?.IsPlayer == true)
                {
                    // give health if player opened the door (medikit)
                    // VARS( m_eoActivator )->health += m_bHealthValue;

                    //TODO
                    //activator.TakeHealth(m_bHealthValue, DMG_GENERIC);
                }

                // play door unlock sounds
                //TODO:
                //PlayLockSounds(pev, &m_ls, FALSE, FALSE);

                DoorGoUp();
            }

            return true;
        }

        /// <summary>
        /// Starts the door going to its "down" position (simply ToggleData->vecPosition1).
        /// </summary>
        private void DoorGoDown()
        {
            if ((SpawnFlags & SF.DoorSilent) == 0)
            {
                if (m_toggle_state != ToggleState.GoingUp && m_toggle_state != ToggleState.GoingDown)
                {
                    //TODO
                    //EMIT_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseMoving), 1, ATTN_NORM);
                }
            }

#if DOOR_ASSERT
            Debug.Assert(m_toggle_state == ToggleState.AtTop);
#endif // DOOR_ASSERT

            m_toggle_state = ToggleState.GoingDown;

            SetMoveDone(DoorHitBottom);

            //TODO: make this cleaner
            if (ClassName == "func_door_rotating")//rotating door
                AngularMove(m_vecAngle1, Speed);
            else
                LinearMove(m_vecPosition1, Speed);
        }

        /// <summary>
        /// The door has reached the "down" position.  Back to quiescence.
        /// </summary>
        /// <param name=""></param>
        private void DoorHitBottom()
        {
            if ((SpawnFlags & SF.DoorSilent) == 0)
            {
                //TODO
                //STOP_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseMoving));
                //EMIT_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseArrived), 1, ATTN_NORM);
            }

            Debug.Assert(m_toggle_state == ToggleState.GoingDown);
            m_toggle_state = ToggleState.AtBottom;

            // Re-instate touch method, cycle is complete
            if ((SpawnFlags & SF.DoorUseOnly) != 0)
            {
                // use only door
                SetTouch(null);
            }
            else // touchable door
            {
                SetTouch(DoorTouch);
            }

            //TODO
            //SUB_UseTargets(m_hActivator, USE_TOGGLE, 0); // this isn't finished

            // Fire the close target (if startopen is set, then "top" is closed) - netname is the close target
            //TODO
            //if (pev->netname && (SpawnFlags & SF.DoorStartsOpen) == 0)
            {
                //TODO
                //FireTargets(STRING(pev->netname), m_hActivator, this, USE_TOGGLE, 0);
            }
        }

        /// <summary>
        /// Starts the door going to its "up" position (simply ToggleData->vecPosition2).
        /// </summary>
        /// <param name=""></param>
        private void DoorGoUp()
        {
            // It could be going-down, if blocked.
            Debug.Assert(m_toggle_state == ToggleState.AtBottom || m_toggle_state == ToggleState.GoingDown);

            // emit door moving and stop sounds on CHAN_STATIC so that the multicast doesn't
            // filter them out and leave a client stuck with looping door sounds!
            if ((SpawnFlags & SF.DoorSilent) == 0)
            {
                if (m_toggle_state != ToggleState.GoingUp && m_toggle_state != ToggleState.GoingDown)
                {
                    //TODO
                    //EMIT_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseMoving), 1, ATTN_NORM);
                }
            }

            m_toggle_state = ToggleState.GoingUp;

            SetMoveDone(DoorHitTop);

            if (ClassName == "func_door_rotating")        // !!! BUGBUG Triggered doors don't work with this yet
            {
                var sign = 1.0f;

                var activator = Activator;

                if (activator != null)
                {
                    if ((SpawnFlags & SF.DoorOneWay) == 0 && MoveDirection.Y != 0)        // Y axis rotation, move away from the player
                    {
                        var vec = activator.Origin - Origin;

                        //			Vector vnext = (pevToucher->origin + (pevToucher->velocity * 10)) - pev->origin;
                        VectorUtils.AngleToVectors(activator.Angles, out var forward, out _, out _);
                        var vnext = (activator.Origin + (forward * 10)) - Origin;

                        if (((vec.X * vnext.Y) - (vec.Y * vnext.X)) < 0)
                        {
                            sign = -1.0f;
                        }
                    }
                }

                AngularMove(m_vecAngle2 * sign, Speed);
            }
            else
            {
                LinearMove(m_vecPosition2, Speed);
            }
        }

        /// <summary>
        /// The door has reached the "up" position.  Either go back down, or wait for another activation.
        /// </summary>
        private void DoorHitTop()
        {
            if ((SpawnFlags & SF.DoorSilent) == 0)
            {
                //TODO
                //STOP_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseMoving));
                //EMIT_SOUND(ENT(pev), CHAN_STATIC, (char*)STRING(pev->noiseArrived), 1, ATTN_NORM);
            }

            Debug.Assert(m_toggle_state == ToggleState.GoingUp);
            m_toggle_state = ToggleState.AtTop;

            // toggle-doors don't come down automatically, they wait for refire.
            if ((SpawnFlags & SF.DoorNoAutoReturn) != 0)
            {
                // Re-instate touch method, movement is complete
                if ((SpawnFlags & SF.DoorUseOnly) == 0)
                {
                    SetTouch(DoorTouch);
                }
            }
            else
            {
                // In flWait seconds, DoorGoDown will fire, unless wait is -1, then door stays open
                NextThink = LastThinkTime + m_flWait;
                SetThink(DoorGoDown);

                if (m_flWait == -1)
                {
                    NextThink = -1;
                }
            }

            // Fire the close target (if startopen is set, then "top" is closed) - netname is the close target
            //TODO
            //if (pev->netname && (SpawnFlags & SF.DoorStartsOpen) != 0)
            {
                //FireTargets(STRING(pev->netname), m_hActivator, this, USE_TOGGLE, 0);
            }

            //SUB_UseTargets(m_hActivator, USE_TOGGLE, 0); // this isn't finished
        }
    }
}
