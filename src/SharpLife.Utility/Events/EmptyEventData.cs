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

namespace SharpLife.Utility.Events
{
    /// <summary>
    /// Event data class that provides no extra data
    /// You should never have to create an instance of this yourself, use the static <see cref="Instance"/> member
    /// </summary>
    public sealed class EmptyEventData : EventData
    {
        public static EmptyEventData Instance { get; } = new EmptyEventData();
    }
}
