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
    public enum FixAngleMode
    {
        None = 0,

        /// <summary>
        /// Set view angles to entity angles
        /// </summary>
        Set = 1,

        /// <summary>
        /// Add angular velocity to view angles
        /// AngularVelocity yaw angle is set to 0 after applying velocity
        /// </summary>
        AddAVelocity = 2,
    }
}
