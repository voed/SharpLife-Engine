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

namespace SharpLife.Game.Shared.Entities.MetaData
{
    /// <summary>
    /// Indicates that a class is networkable and that it should be registered
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NetworkableAttribute : Attribute
    {
        /// <summary>
        /// If true, the base class type should be used for networking
        /// Classes marked with this are not themselves networkable, but use only base class networkable properties
        /// </summary>
        public bool UseBaseType { get; set; }

        /// <summary>
        /// If provided, this is the name of the server version of a client networkable type
        /// </summary>
        public string MapsFromType { get; set; }
    }
}
