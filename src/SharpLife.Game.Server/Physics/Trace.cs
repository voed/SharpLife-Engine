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


using SharpLife.Game.Server.Entities;
using System.Numerics;

namespace SharpLife.Game.Server.Physics
{
    public struct Trace
    {
        /// <summary>
        /// if true, plane is not valid
        /// </summary>
        public bool AllSolid;

        /// <summary>
        /// if true, the initial point was in a solid area
        /// </summary>
        public bool StartSolid;
        public bool InOpen, InWater;

        /// <summary>
        /// time completed, 1.0 = didn't hit anything
        /// </summary>
        public float Fraction;

        /// <summary>
        /// final position
        /// </summary>
        public Vector3 EndPosition;

        /// <summary>
        /// surface normal at impact
        /// </summary>
        public Plane Plane;

        /// <summary>
        /// entity the surface is on
        /// </summary>
        public BaseEntity Entity;

        /// <summary>
        /// 0 == generic, non zero is specific body part
        /// </summary>
        public int HitGroup;
    }
}
