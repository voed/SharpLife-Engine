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

using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Frames;
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using SharpLife.Utility.Collections.Generic;
using System;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission
{
    /// <summary>
    /// Handles the transmitting of frames
    /// Keeps tracks of frames pending transmission
    /// </summary>
    internal sealed class NetworkFrameListTransmitter : INetworkFrameListTransmitter
    {
        private readonly NetworkObjectListTransmitter _listTransmitter;

        private readonly CircularBuffer<FrameList> _frameListLists;

        public IFrameListTransmitterListener Listener { get; }

        public NetworkFrameListTransmitter(NetworkObjectListTransmitter listTransmitter, int maxFrameLists, IFrameListTransmitterListener listener)
        {
            _listTransmitter = listTransmitter ?? throw new ArgumentNullException(nameof(listTransmitter));
            _frameListLists = new CircularBuffer<FrameList>(maxFrameLists);
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
        }

        internal void AddList(FrameList list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            _frameListLists.Add(list);
        }

        public NetworkObjectListFrameListUpdate SerializeCurrentFrameList()
        {
            if (_frameListLists.Count == 0)
            {
                throw new InvalidOperationException("No frame list to serialize");
            }

            var previous = _frameListLists.Count > 1 ? _frameListLists[_frameListLists.Count - 2] : null;

            var current = _frameListLists[_frameListLists.Count - 1];

            return current.SerializeFrames(_listTransmitter, previous);
        }
    }
}
