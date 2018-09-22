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

namespace SharpLife.Game.Shared.Entities.EntityList
{
    public interface IEntityList
    {
        IEntity GetEntity(in ObjectHandle handle);

        /// <summary>
        /// Gets a handle to the first entity in the list, or a default handle if no entities exist
        /// </summary>
        /// <returns></returns>
        ObjectHandle GetFirstEntity();

        /// <summary>
        /// Gets a handle to the next entity in the list after the entity represented by the given handle, or a default handle if there are no more entities in the list
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        ObjectHandle GetNextEntity(in ObjectHandle handle);
    }
}
