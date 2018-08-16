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


namespace SharpLife.Engine.Server.Clients
{
    /// <summary>
    /// Which stage of the client setup stage the client is in
    /// </summary>
    public enum ServerClientSetupStage
    {
        NotConnected = 0,

        /// <summary>
        /// Client is in the process of connecting
        /// </summary>
        Connecting,

        /// <summary>
        /// Awaiting confirmation from the client to start setting up the networked data
        /// </summary>
        AwaitingSetupStart,

        /// <summary>
        /// Awaiting a request from the client to start sending resources
        /// </summary>
        AwaitingResourceTransmissionStart,

        /// <summary>
        /// Sending binary metadata for string lists
        /// </summary>
        SendingStringListsBinaryMetaData,

        /// <summary>
        /// Sending string lists to the client
        /// </summary>
        SendingStringLists,

        /// <summary>
        /// Sending object list type meta data to the client
        /// </summary>
        SendingObjectListTypeMetaData,

        /// <summary>
        /// Sending object list list meta data to the client
        /// </summary>
        SendingObjectListListMetaData,

        /// <summary>
        /// Client is fully connected and set up
        /// </summary>
        Connected,
    }
}
