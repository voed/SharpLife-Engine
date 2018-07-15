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

using Veldrid;

namespace SharpLife.Input
{
    /// <summary>
    /// Contains the state of a mouse button
    /// </summary>
    public struct MouseButtonState
    {
        public MouseButton Button { get; }

        public bool Down { get; set; }

        /// <summary>
        /// The last time the state of this button changed
        /// </summary>
        public uint ChangeTimestamp { get; set; }

        public MouseButtonState(MouseButton button)
        {
            Button = button;
            Down = false;
            ChangeTimestamp = 0;
        }
    }
}
