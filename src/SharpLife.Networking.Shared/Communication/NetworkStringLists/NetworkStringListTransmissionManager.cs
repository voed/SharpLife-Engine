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
using Google.Protobuf.Reflection;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public sealed class NetworkStringListTransmissionManager
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

        private readonly NetworkStringListManager _listManager = new NetworkStringListManager();

        private readonly Dictionary<int, ListData> _listData = new Dictionary<int, ListData>();

        private readonly Dictionary<MessageDescriptor, uint> _binaryDescriptorToIndex = new Dictionary<MessageDescriptor, uint>();

        public int Count => _listManager.Count;

        public INetworkStringList CreateList(string name)
        {
            var list = _listManager.CreateList(name);

            var internalList = list as NetworkStringList;

            _listData.Add(internalList.Index, new ListData());

            list.OnStringAdded += OnStringAdded;
            list.OnBinaryDataChanged += OnBinaryDataChanged;

            return list;
        }

        public void Clear()
        {
            _binaryDescriptorToIndex.Clear();
            _listManager.Clear();
        }

        /// <summary>
        /// Registers a binary type for use with a string's binary data
        /// </summary>
        /// <param name="descriptor"></param>
        public void RegisterBinaryType(MessageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!_binaryDescriptorToIndex.ContainsKey(descriptor))
            {
                _binaryDescriptorToIndex.Add(descriptor, (uint)_binaryDescriptorToIndex.Count);
            }
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

        public List<NetworkStringListBinaryMetaData> CreateBinaryTypesMessages()
        {
            var list = new List<NetworkStringListBinaryMetaData>(_binaryDescriptorToIndex.Count);

            foreach (var descriptor in _binaryDescriptorToIndex)
            {
                list.Add(new NetworkStringListBinaryMetaData
                {
                    Index = descriptor.Value,
                    TypeName = descriptor.Key.ClrType.FullName
                });
            }

            return list;
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

                message.DataType = _binaryDescriptorToIndex[binaryData.Descriptor];
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

            var list = _listManager[index];

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

            for (var i = 0; i < _listManager.Count; ++i)
            {
                var listData = _listData[i];

                if (listData.HasChanges)
                {
                    var list = _listManager[i];

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
