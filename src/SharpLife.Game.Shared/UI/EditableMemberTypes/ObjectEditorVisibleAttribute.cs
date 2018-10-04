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

using System;

namespace SharpLife.Game.Shared.UI.EditableMemberTypes
{
    /// <summary>
    /// Used to control the visibility of members in the object editor
    /// If provided on a class, controls the default setting for all members
    /// If provided on a member, controls the visibility of that member
    /// If not provided at all, members are visible by default
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public sealed class ObjectEditorVisibleAttribute : Attribute
    {
        public bool Visible { get; set; } = true;
    }
}
