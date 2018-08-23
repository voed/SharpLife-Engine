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
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public sealed class NetworkStringListTransmitter : BaseNetworkStringListManager<BinaryDataTransmissionDescriptorSet>
    {
        private class ListData
        {
            public List<int> addedStrings = new List<int>();

            public List<int> changedStrings = new List<int>();

            public bool HasChanges => addedStrings.Count > 0 || changedStrings.Count > 0;

            public void Clear()
            {
                addedStrings.Clear();
                changedStrings.Clear();
            }
        }

        private readonly Dictionary<int, ListData> _listData = new Dictionary<int, ListData>();

        public NetworkStringListTransmitter(BinaryDataTransmissionDescriptorSet descriptorSet)
            : base(descriptorSet)
        {
        }

        internal override void OnListCreated(NetworkStringList list)
        {
            _listData.Add(list.Index, new ListData());

            list.OnStringAdded += OnStringAdded;
            list.OnBinaryDataChanged += OnBinaryDataChanged;
        }

        private void OnStringAdded(IReadOnlyNetworkStringList stringList, int index)
        {
            var internalList = stringList as NetworkStringList;

            var data = _listData[internalList.Index];

            data.addedStrings.Add(index);
        }

        private void OnBinaryDataChanged(IReadOnlyNetworkStringList stringList, int index)
        {
            var internalList = stringList as NetworkStringList;

            var data = _listData[internalList.Index];

            data.changedStrings.Add(index);
        }

        private ListBinaryData CreateBinaryDataFor(Stream stream, NetworkStringList list, int index)
        {
            var binaryData = list.GetBinaryData(index);

            var message = new ListBinaryData();

            if (binaryData != null)
            {
                binaryData.WriteTo(stream);

                stream.Position = 0;
                message.BinaryData = ByteString.FromStream(stream);
                stream.SetLength(0);

                message.DataType = _binaryDataDescriptorSet.GetDescriptorIndex(binaryData.Descriptor);
            }

            return message;
        }

        private ListStringData CreateStringDataFor(Stream stream, NetworkStringList list, int index)
        {
            return new ListStringData
            {
                Value = list[index],
                BinaryData = CreateBinaryDataFor(stream, list, index)
            };
        }

        /// <summary>
        /// Create a full update for the given table
        /// All data is sent
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public NetworkStringListFullUpdate CreateFullUpdate(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var list = this[index];

            var update = new NetworkStringListFullUpdate
            {
                ListId = (uint)index,
                Name = list.Name
            };

            var binaryDataBuffer = new MemoryStream();

            for (var i = 0; i < list.Count; ++i)
            {
                update.Strings.Add(CreateStringDataFor(binaryDataBuffer, list, i));
            }

            return update;
        }

        /// <summary>
        /// Create list updates and update listeners
        /// </summary>
        public List<NetworkStringListUpdate> CreateUpdates()
        {
            var updates = new List<NetworkStringListUpdate>();

            var binaryDataBuffer = new MemoryStream();

            for (var i = 0; i < Count; ++i)
            {
                var listData = _listData[i];

                if (listData.HasChanges)
                {
                    var list = this[i];

                    var update = new NetworkStringListUpdate
                    {
                        ListId = (uint)i
                    };

                    foreach (var added in listData.addedStrings)
                    {
                        update.Strings.Add(CreateStringDataFor(binaryDataBuffer, list, added));
                    }

                    foreach (var changed in listData.changedStrings)
                    {
                        update.Updates.Add(new ListStringDataUpdate
                        {
                            Index = (uint)changed,
                            BinaryData = CreateBinaryDataFor(binaryDataBuffer, list, changed)
                        });
                    }

                    listData.Clear();

                    updates.Add(update);
                }
            }

            return updates;
        }
    }
}
