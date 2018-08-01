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

using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkStringLists
{
    public sealed class NetworkStringListTransmissionManager
    {
        private class ListData
        {
            public List<int> addedStrings = new List<int>();

            public bool HasChanges => addedStrings.Count > 0;

            public void Clear()
            {
                addedStrings.Clear();
            }
        }

        private readonly NetworkStringListManager _listManager = new NetworkStringListManager();

        private readonly Dictionary<int, ListData> _listData = new Dictionary<int, ListData>();

        public int Count => _listManager.Count;

        public INetworkStringList CreateList(string name)
        {
            var list = _listManager.CreateList(name);

            var internalList = list as NetworkStringList;

            _listData.Add(internalList.Index, new ListData());

            list.OnStringAdded += OnStringAdded;

            return list;
        }

        public void Clear()
        {
            _listManager.Clear();
        }

        private void OnStringAdded(IReadOnlyNetworkStringList stringList, int index)
        {
            var internalList = stringList as NetworkStringList;

            var data = _listData[internalList.Index];

            data.addedStrings.Add(index);
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

            for (var i = 0; i < list.Count; ++i)
            {
                update.Strings.Add(new ListStringData
                {
                    Value = list[i]
                });
            }

            return update;
        }

        /// <summary>
        /// Create list updates and update listeners
        /// </summary>
        public List<NetworkStringListUpdate> CreateUpdates()
        {
            var updates = new List<NetworkStringListUpdate>();

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
                        update.Strings.Add(new ListStringData
                        {
                            Value = list[added]
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
