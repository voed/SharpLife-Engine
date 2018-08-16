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

using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Frames
{
    /// <summary>
    /// A list of frames for a specific time
    /// </summary>
    internal sealed class FrameList
    {
        private readonly List<Frame> _frames = new List<Frame>();

        public IReadOnlyList<Frame> Frames => _frames;

        public Frame FindFrameByListId(int listId)
        {
            return _frames.Find(frame => frame.ListId == listId);
        }

        public void AddFrame(Frame frame)
        {
            _frames.Add(frame);
        }

        /// <summary>
        /// Serializes all frames to a message
        /// </summary>
        /// <param name="objectListManager"></param>
        /// <param name="previousFrames"></param>
        public NetworkObjectListFrameListUpdate SerializeFrames(BaseNetworkObjectListManager objectListManager, FrameList previousFrames)
        {
            var frameListMessage = new NetworkObjectListFrameListUpdate();

            foreach (var frame in _frames)
            {
                var objectList = objectListManager.InternalFindListById(frame.ListId);

                var previousFrame = previousFrames?.FindFrameByListId(frame.ListId);

                var result = frame.Serialize(objectList, previousFrame);

                //TODO: check if anything was written to allow discarding of empty frames

                frameListMessage.Frames.Add(result);
            }

            return frameListMessage;
        }

        public static FrameList DeserializeFrameList(BaseNetworkObjectListManager listManager, FrameList previousFrames, NetworkObjectListFrameListUpdate frameListMessage)
        {
            var frameList = new FrameList();

            foreach (var frameMessage in frameListMessage.Frames)
            {
                if (listManager.InternalFindListById((int)frameMessage.ListId) == null)
                {
                    throw new InvalidOperationException($"List with id {frameMessage.ListId} does not exist");
                }

                var previousFrame = previousFrames?.FindFrameByListId((int)frameMessage.ListId);

                frameList.AddFrame(Frame.Deserialize(frameMessage, listManager.TypeRegistry, previousFrame));
            }

            return frameList;
        }
    }
}
