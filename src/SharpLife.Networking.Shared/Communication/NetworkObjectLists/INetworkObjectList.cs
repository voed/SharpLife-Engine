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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists
{
    public interface INetworkObjectList
    {
        string Name { get; }

        int Id { get; }

        INetworkObject FindNetworkObjectForObject(INetworkable networkableObject);

        INetworkObject GetNetworkObjectById(int id);

        /// <summary>
        /// Creates a new network object for a networkable object
        /// For transmitters, the object must have a handle set for it
        /// For receivers, the handle will be set during deserialization
        /// It is the responsibility of the list user to ensure that no duplicate ids are issued to object instances
        /// </summary>
        /// <param name="networkableObject"></param>
        /// <returns></returns>
        INetworkObject CreateNetworkObject(INetworkable networkableObject);

        void DestroyNetworkObject(INetworkObject networkObject);
    }
}
