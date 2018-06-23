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

namespace SharpLife.Engine.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Console variable change filter
    /// </summary>
    public interface IConVarFilter
    {
        /// <summary>
        /// Invoked when a variable is about to change values, filters the new values
        /// </summary>
        /// <param name="stringValue">New string value</param>
        /// <param name="floatValue">New float value</param>
        /// <returns>Whether to allow the change at all</returns>
        bool Filter(ref string stringValue, ref float floatValue);
    }
}
