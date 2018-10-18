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

namespace SharpLife.Game.Shared.Entities
{
    /// <summary>
    /// <see cref="SharedBaseEntity.MoveType"/> values
    /// </summary>
    public enum MoveType
    {
        /// <summary>
        /// never moves
        /// </summary>
        None = 0,
        //AngleNoclip	= 1,
        //AngleClip     = 2,

        /// <summary>
        /// Player only - moving on the ground
        /// </summary>
        Walk = 3,

        /// <summary>
        /// gravity, special edge handling -- monsters use this
        /// </summary>
        Step = 4,

        /// <summary>
        /// No gravity, but still collides with stuff
        /// </summary>
        Fly = 5,

        /// <summary>
        /// gravity/collisions
        /// </summary>
        Toss = 6,

        /// <summary>
        /// no clip to world, push and crush
        /// </summary>
        Push = 7,

        /// <summary>
        /// No gravity, no collisions, still do velocity/avelocity
        /// </summary>
        Noclip = 8,

        /// <summary>
        /// extra size to monsters
        /// </summary>
        FlyMissile = 9,

        /// <summary>
        /// Just like Toss, but reflect velocity when contacting surfaces
        /// </summary>
        Bounce = 10,

        /// <summary>
        /// bounce w/o gravity
        /// </summary>
        BounceMissile = 11,

        /// <summary>
        /// track movement of aiment
        /// </summary>
        Follow = 12,

        /// <summary>
        /// BSP model that needs physics/world collisions (uses nearest hull for world collision)
        /// </summary>
        PushStep = 13,
    }
}
