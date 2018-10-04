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

namespace SharpLife.Game.Shared.UI.EditableMemberTypes
{
    /// <summary>
    /// How to display Vector3 values in the object editor
    /// </summary>
    public enum Vector3DisplayFormat
    {
        /// <summary>
        /// Display as three text edits accepting float values
        /// </summary>
        Floats = 0,

        /// <summary>
        /// Display as a 24 bit color value ([0, 255])
        /// </summary>
        Color24,
    }
}
