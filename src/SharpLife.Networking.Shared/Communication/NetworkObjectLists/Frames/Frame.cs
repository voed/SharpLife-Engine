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
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Frames
{
    /// <summary>
    /// An entire frame's worth of updates for a single list
    /// </summary>
    internal sealed class Frame
    {
        private readonly List<ObjectDestruction> _destroyedObjects = new List<ObjectDestruction>();

        private readonly List<ObjectUpdate> _updates = new List<ObjectUpdate>();

        public int ListId { get; }

        public IReadOnlyList<ObjectDestruction> DestroyedObjects => _destroyedObjects;

        public IReadOnlyList<ObjectUpdate> Updates => _updates;

        public Frame(int listId)
        {
            ListId = listId;
        }

        private Frame(int listId, List<ObjectDestruction> destroyedObjects)
        {
            ListId = listId;
            _destroyedObjects = destroyedObjects ?? throw new ArgumentNullException(nameof(destroyedObjects));
        }

        public ObjectUpdate FindUpdateByObjectId(int id)
        {
            foreach (var update in _updates)
            {
                if (update.ObjectHandle.Id == id)
                {
                    return update;
                }
            }

            return null;
        }

        public void CreateObjectDestruction(int id)
        {
            _destroyedObjects.Add(new ObjectDestruction { ObjectId = (uint)id });
        }

        public void CreateUpdate(NetworkObject networkObject, Frame previousFrame)
        {
            if (networkObject == null)
            {
                throw new ArgumentNullException(nameof(networkObject));
            }

            var previousUpdate = previousFrame?.FindUpdateByObjectId(networkObject.Handle.Id);

            _updates.Add(new ObjectUpdate(networkObject.Handle, networkObject.MetaData, networkObject.TakeSnapshot(previousUpdate?.Snapshot)));
        }

        private void DeserializeUpdate(ByteString data, TypeRegistry typeRegistry, Frame previousFrame)
        {
            using (var codedStream = new CodedInputStream(data.ToByteArray()))
            {
                var objectId = ObjectUpdate.DeserializeObjectId(codedStream);
                var serialNumber = ObjectUpdate.DeserializeSerialNumber(codedStream);
                var typeId = ObjectUpdate.DeserializeTypeId(codedStream);

                var metaData = typeRegistry.FindMetaDataByTransmitterId(typeId);

                var previousUpdate = previousFrame?.FindUpdateByObjectId(objectId);

                var update = ObjectUpdate.DeserializeFromStream(codedStream, new ObjectHandle(objectId, serialNumber), metaData, previousUpdate);

                _updates.Add(update);
            }
        }

        public FrameMessage Serialize(NetworkObjectList objectList, Frame previousFrame)
        {
            var updateList = new List<MemoryStream>();

            foreach (var update in _updates)
            {
                var data = update.Serialize(objectList.InternalGetNetworkObjectById(update.ObjectHandle.Id), previousFrame?.FindUpdateByObjectId(update.ObjectHandle.Id));

                //TODO: figure out if there's a better way to handle change detection during serialization
                //if (data.ContainsChanges)
                {
                    data.Memory.Position = 0;
                    updateList.Add(data.Memory);
                }
            }

            var stream = new MemoryStream();

            using (var codedStream = new CodedOutputStream(stream, true))
            {
                codedStream.WriteInt32(updateList.Count);

                foreach (var update in updateList)
                {
                    codedStream.WriteBytes(ByteString.FromStream(update));
                    update.Dispose();
                }
            }

            stream.Position = 0;

            var frameMessage = new FrameMessage
            {
                ListId = (uint)ListId,

                ObjectUpdates = ByteString.FromStream(stream)
            };

            stream.Dispose();

            frameMessage.ObjectsDestroyed.AddRange(_destroyedObjects);

            return frameMessage;
        }

        public static Frame Deserialize(FrameMessage frameMessage, TypeRegistry typeRegistry, Frame previousFrame)
        {
            var frame = new Frame(
                (int)frameMessage.ListId,
                frameMessage.ObjectsDestroyed.ToList());

            using (var stream = new MemoryStream())
            {
                frameMessage.ObjectUpdates.WriteTo(stream);

                stream.Position = 0;

                using (var codedStream = new CodedInputStream(stream))
                {
                    var updateCount = codedStream.ReadInt32();

                    for (var i = 0; i < updateCount; ++i)
                    {
                        var update = codedStream.ReadBytes();

                        frame.DeserializeUpdate(update, typeRegistry, previousFrame);
                    }

                    return frame;
                }
            }
        }
    }
}
