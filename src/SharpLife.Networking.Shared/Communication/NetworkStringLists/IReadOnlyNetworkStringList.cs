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
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public interface IReadOnlyNetworkStringList : IEnumerable<string>
    {
        /// <summary>
        /// Gets the name of the list
        /// The name is used to identify the list across the network
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The number of strings in the list
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a string by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">If the index is out of range</exception>
        string this[int index]
        {
            get;
        }

        /// <summary>
        /// Invoked when a new string is added
        /// The index of the string is passed
        /// </summary>
        event Action<IReadOnlyNetworkStringList, int> OnStringAdded;

        /// <summary>
        /// Invoked when the binary data for a string changes
        /// Not invoked when the string is initially added
        /// </summary>
        event Action<IReadOnlyNetworkStringList, int> OnBinaryDataChanged;

        /// <summary>
        /// Gets the index of the given string, or -1 if the string is not in the list
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IndexOf(string value);

        IMessage GetBinaryData(string value);

        IMessage GetBinaryData(int index);

        /// <summary>
        /// Gets the user data object associated with this string
        /// User data is not networked
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        object GetUserData(string value);

        /// <summary>
        /// <see cref="GetUserData(string)"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        object GetUserData(int index);

        /// <summary>
        /// Sets the user data object associated with this string
        /// User data is not networked
        /// </summary>
        /// <param name="value"></param>
        /// <param name="userData"></param>
        void SetUserData(string value, object userData);

        /// <summary>
        /// <see cref="SetUserData(string, object)"/>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="userData"></param>
        void SetUserData(int index, object userData);
    }
}
