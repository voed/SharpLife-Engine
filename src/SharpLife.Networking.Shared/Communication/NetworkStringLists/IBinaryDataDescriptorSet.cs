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

using Google.Protobuf.Reflection;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    /// <summary>
    /// A set of message descriptors used for binary data transmission
    /// </summary>
    public interface IBinaryDataDescriptorSet
    {
        bool Contains(MessageDescriptor descriptor);

        /// <summary>
        /// Registers a binary type for use with a string's binary data
        /// </summary>
        /// <param name="descriptor"></param>
        void Add(MessageDescriptor descriptor);
    }
}
