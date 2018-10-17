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

using SharpLife.Networking.Shared.Communication.NetworkObjectLists;

namespace SharpLife.Game.Shared.Entities
{
    public interface IEntity : INetworkable
    {
        string ClassName { get; set; }

        /// <summary>
        /// Called when this entity is created
        /// </summary>
        void OnConstruct();

        /// <summary>
        /// Called when this entity is being destroyed
        /// </summary>
        void OnDestruct();
    }
}
