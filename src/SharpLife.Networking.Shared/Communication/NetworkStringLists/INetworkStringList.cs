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

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public interface INetworkStringList : IReadOnlyNetworkStringList
    {
        /// <summary>
        /// Adds a string to the list
        /// If the string already exists, the index for it is returned
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Index of the string</returns>
        /// <exception cref="System.ArgumentNullException">If the given string is null</exception>
        int Add(string value);
    }
}
