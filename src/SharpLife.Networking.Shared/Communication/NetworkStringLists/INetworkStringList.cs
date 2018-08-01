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

using Google.Protobuf;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public interface INetworkStringList : IReadOnlyNetworkStringList
    {
        /// <summary>
        /// Adds a string to the list
        /// If the string already exists, the index for it is returned
        /// </summary>
        /// <param name="value"></param>
        /// <param name="binaryData">Optional binary data</param>
        /// <returns>Index of the string</returns>
        /// <exception cref="System.ArgumentNullException">If the given string is null</exception>
        int Add(string value, IMessage binaryData = null);

        /// <summary>
        /// Sets binary data for the given string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="binaryData">Binary data to set. Pass null to clear</param>
        void SetBinaryData(string value, IMessage binaryData);

        /// <summary>
        /// Sets binary data for the given string
        /// </summary>
        /// <param name="index"></param>
        /// <param name="binaryData">Binary data to set. Pass null to clear</param>
        void SetBinaryData(int index, IMessage binaryData);
    }
}
