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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NetworkedAttribute : Attribute
    {
        /// <summary>
        /// If true, user code will inform the network object when this member is changed
        /// </summary>
        public bool UsesChangeNotification { get; set; }
    }
}
