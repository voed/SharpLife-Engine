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
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission
{
    public sealed class NetworkObjectListTransmitter : BaseNetworkObjectListManager
    {
        private readonly List<NetworkFrameListTransmitter> _transmitters = new List<NetworkFrameListTransmitter>();

        private readonly int _maxFrameLists;

        public NetworkObjectListTransmitter(TypeRegistry typeRegistry, int maxFrameLists)
            : base(typeRegistry)
        {
            if (maxFrameLists < 2)
            {
                throw new ArgumentOutOfRangeException("At least 2 framelists are required for transmission", nameof(maxFrameLists));
            }

            _maxFrameLists = maxFrameLists;
        }

        /// <summary>
        /// Creates a new list with the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public INetworkObjectList CreateList(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Network object list name should contain at least one valid character", nameof(name));
            }

            var objectList = new NetworkObjectList(this, name, _objectLists.Count);

            _objectLists.Add(objectList);

            return objectList;
        }

        /// <summary>
        /// Creates a new network frame transmitter
        /// </summary>
        /// <param name="listener">Listener to use when determining which objects to include in a list</param>
        /// <returns></returns>
        public INetworkFrameListTransmitter CreateTransmitter(IFrameListTransmitterListener listener)
        {
            var transmitter = new NetworkFrameListTransmitter(this, _maxFrameLists, listener);

            _transmitters.Add(transmitter);

            return transmitter;
        }

        public void DestroyTransmitter(INetworkFrameListTransmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException(nameof(transmitter));
            }

            if (!_transmitters.Remove((NetworkFrameListTransmitter)transmitter))
            {
                throw new InvalidOperationException("Network frame list transmitter is not managed by this system");
            }
        }

        /// <summary>
        /// Creates a frame for a specific transmitter and list
        /// </summary>
        /// <param name="transmitter"></param>
        /// <param name="objectList"></param>
        /// <returns></returns>
        private Frame CreateFrame(NetworkFrameListTransmitter transmitter, NetworkObjectList objectList)
        {
            var frame = new Frame(objectList.Id);

            transmitter.Listener.OnBeginProcessList(objectList);

            foreach (var networkObject in objectList.InternalNetworkObjects)
            {
                if (networkObject.Destroyed)
                {
                    frame.CreateObjectDestruction(networkObject.Handle.Id);

                    //Don't remove a destroyed object's data from previous frames, we may need to reconstruct it for lag compensation
                }
                else
                {
                    if (transmitter.Listener.FilterNetworkObject(objectList, networkObject))
                    {
                        frame.CreateUpdate(networkObject);
                    }
                }
            }

            transmitter.Listener.OnEndProcessList(objectList);

            return frame;
        }

        /// <summary>
        /// Creates frames for each object list for each transmitter
        /// </summary>
        public void CreateFramesForTransmitters()
        {
            //Only create frames for transmitters that are ready to transmit
            var transmitters = _transmitters.Where(transmitter => transmitter.Listener.CanTransmit).ToList();

            //Create a frame list for each transmitter
            var frameListList = new FrameList[transmitters.Count];

            for (var i = 0; i < transmitters.Count; ++i)
            {
                frameListList[i] = new FrameList();
            }

            //For each object list, create a frame for each transmitter
            //This ensures that each list is fully processed, allowing us to inform listeners without having to repeatedly signal begin/end
            foreach (var objectList in _objectLists)
            {
                for (var i = 0; i < transmitters.Count; ++i)
                {
                    var transmitter = transmitters[i];
                    var frameList = frameListList[i];

                    frameList.AddFrame(CreateFrame(transmitter, objectList));
                }

                objectList.PostFramesCreated();
            }

            for (var i = 0; i < transmitters.Count; ++i)
            {
                transmitters[i].AddList(frameListList[i]);
            }
        }
    }
}
