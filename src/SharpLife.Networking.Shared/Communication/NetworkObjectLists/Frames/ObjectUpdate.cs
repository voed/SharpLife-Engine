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
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion;
using System;
using System.IO;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Frames
{
    internal sealed class ObjectUpdate
    {
        public int ObjectId { get; }

        public TypeMetaData MetaData { get; }

        public object[] Snapshot { get; }

        public ObjectUpdate(int objectId, TypeMetaData metaData, object[] snapshot)
        {
            ObjectId = objectId;
            MetaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        internal ObjectUpdateResult Serialize(NetworkObject networkObject, ObjectUpdate previousUpdate)
        {
            if (networkObject == null)
            {
                throw new ArgumentNullException(nameof(networkObject));
            }

            var previousSnapshot = previousUpdate?.Snapshot;

            var memory = new MemoryStream();
            var stream = new CodedOutputStream(memory, true);

            var data = new ObjectUpdateResult
            {
                Memory = memory,
                IsDelta = previousSnapshot != null
            };

            stream.WriteInt32(ObjectId);
            stream.WriteUInt32(MetaData.Id);

            stream.WriteBool(data.IsDelta);

            //TODO: if nothing has to be networked, don't network the updates (still need create & destroy)
            if (data.IsDelta)
            {
                data.ContainsChanges = SerializeDelta(networkObject, previousSnapshot, stream);
            }
            else
            {
                data.ContainsChanges = SerializeFull(stream);
            }

            stream.Dispose();

            return data;
        }

        private bool SerializeDelta(NetworkObject networkObject, object[] previousSnapshot, CodedOutputStream stream)
        {
            var changes = false;

            for (var i = 0; i < MetaData.Members.Count; ++i)
            {
                var member = MetaData.Members[i];

                if (!member.ChangeNotificationIndex.HasValue || networkObject.ChangeNotifications[member.ChangeNotificationIndex.Value])
                {
                    changes = member.MetaData.Converter.EncodeAndWrite(Snapshot[i], previousSnapshot[i], stream) || changes;
                }
                else
                {
                    //Write unchanged values for each member
                    for (var unchanged = 0; unchanged < member.MetaData.Converter.MemberCount; ++unchanged)
                    {
                        ConversionUtils.AddUnchangedValue(stream);
                    }
                }
            }

            return changes;
        }

        private bool SerializeFull(CodedOutputStream stream)
        {
            for (var i = 0; i < MetaData.Members.Count; ++i)
            {
                var member = MetaData.Members[i];

                member.MetaData.Converter.Write(Snapshot[i], stream);
            }

            return true;
        }

        internal static int DeserializeObjectId(CodedInputStream stream)
        {
            return stream.ReadInt32();
        }

        internal static uint DeserializeTypeId(CodedInputStream stream)
        {
            return stream.ReadUInt32();
        }

        internal static ObjectUpdate DeserializeFromStream(CodedInputStream stream, int objectId, TypeMetaData metaData, ObjectUpdate previousUpdate)
        {
            var snapshot = metaData.AllocateSnapshot();

            //Is it a delta?
            if (stream.ReadBool())
            {
                if (previousUpdate == null)
                {
                    throw new InvalidOperationException($"Object with id {objectId} ({metaData.Type.FullName}) received delta update, but no previous update to delta from");
                }

                var previousSnapshot = previousUpdate.Snapshot;

                for (var i = 0; i < metaData.Members.Count; ++i)
                {
                    var member = metaData.Members[i];

                    if (member.MetaData.Converter.ReadAndDecode(stream, previousSnapshot[i], out var result))
                    {
                        snapshot[i] = result;
                    }
                    else
                    {
                        snapshot[i] = previousSnapshot[i];
                    }
                }
            }
            else
            {
                //previousUpdate can be non-null here, its contents will not be used

                for (var i = 0; i < metaData.Members.Count; ++i)
                {
                    var member = metaData.Members[i];

                    //Full updates can contain deltas, in which case we can just use the default value provided by the Read method
                    //So don't check the return value
                    member.MetaData.Converter.Read(stream, out var result);

                    snapshot[i] = result;
                }
            }

            return new ObjectUpdate(objectId, metaData, snapshot);
        }
    }
}
