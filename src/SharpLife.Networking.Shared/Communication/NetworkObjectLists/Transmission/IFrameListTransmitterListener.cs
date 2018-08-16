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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission
{
    public interface IFrameListTransmitterListener
    {
        /// <summary>
        /// Whether this transmitter can transmit
        /// If it can't, no frames will be created
        /// </summary>
        bool CanTransmit { get; }

        /// <summary>
        /// Invoked when a list begins processing
        /// </summary>
        /// <param name="networkObjectList"></param>
        void OnBeginProcessList(INetworkObjectList networkObjectList);

        /// <summary>
        /// Invoked when a list ends processing
        /// </summary>
        /// <param name="networkObjectList"></param>
        void OnEndProcessList(INetworkObjectList networkObjectList);

        /// <summary>
        /// Filters a network object for transmission
        /// </summary>
        /// <param name="networkObjectList"></param>
        /// <param name="networkObject"></param>
        /// <returns></returns>
        bool FilterNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject);
    }
}
