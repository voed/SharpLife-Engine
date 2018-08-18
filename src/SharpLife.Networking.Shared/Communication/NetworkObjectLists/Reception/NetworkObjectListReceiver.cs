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
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using SharpLife.Utility.Collections.Generic;
using System;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception
{
    /// <summary>
    /// Handles the receiving of frames
    /// Keeps tracks of previous frames
    /// </summary>
    public sealed class NetworkObjectListReceiver : BaseNetworkObjectListManager
    {
        private readonly CircularBuffer<FrameList> _frameListLists;

        public IFrameListReceiverListener Listener { get; }

        public NetworkObjectListReceiver(int maxFrameLists, IFrameListReceiverListener listener)
        {
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

        private NetworkObject CreateNetworkObject(NetworkObjectList objectList, int objectId, TypeMetaData metaData)
        {
            var networkableObject = metaData.CreateInstance();

            var networkObject = objectList.InternalCreateNetworkObject(networkableObject, objectId);

            Listener.OnNetworkObjectCreated(objectList, networkObject, networkableObject);

            return networkObject;
        }

        public void DeserializeFrameList(NetworkObjectListFrameListUpdate frameListMessage)
        {
            var previous = _frameListLists.Count > 0 ? _frameListLists[_frameListLists.Count - 1] : null;

            var frameList = FrameList.DeserializeFrameList(this, previous, frameListMessage);

            _frameListLists.Add(frameList);
        }

        public void ApplyCurrentFrame()
        {
            if (_frameListLists.Count == 0)
            {
                throw new InvalidOperationException("No frame lists to apply");
            }

            var frameList = _frameListLists[_frameListLists.Count - 1];

            foreach (var frame in frameList.Frames)
            {
                var objectList = _objectLists[frame.ListId];

                Listener.OnBeginProcessList(objectList);

                foreach (var destruction in frame.DestroyedObjects)
                {
                    var networkObject = objectList.GetNetworkObjectById((int)destruction.ObjectId);

                    Listener.OnNetworkObjectDestroyed(objectList, networkObject, networkObject.Instance);

                    objectList.DestroyNetworkObject(networkObject);

                    //Don't remove a destroyed object's data from previous frames, we may need to reconstruct it for prediction
                }

                foreach (var update in frame.Updates)
                {
                    //Create the object if it does not already exist
                    var networkObject = objectList.InternalGetNetworkObjectById(update.ObjectId) ?? CreateNetworkObject(objectList, update.ObjectId, update.MetaData);

                    Listener.OnBeginUpdateNetworkObject(objectList, networkObject);
                    networkObject.ApplySnapshot(update.Snapshot);
                    Listener.OnEndUpdateNetworkObject(objectList, networkObject);
                }

                objectList.PostFramesReceived();

                Listener.OnEndProcessList(objectList);
            }
        }
    }
}
