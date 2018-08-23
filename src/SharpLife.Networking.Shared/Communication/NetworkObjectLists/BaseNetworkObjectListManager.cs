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

using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists
{
    /// <summary>
    /// Base class for transmitters and receivers of networked object lists
    /// </summary>
    public abstract class BaseNetworkObjectListManager : IBaseNetworkObjectListManager
    {
        private protected readonly List<NetworkObjectList> _objectLists = new List<NetworkObjectList>();

        public TypeRegistry TypeRegistry { get; }

        public int ListCount => _objectLists.Count;

        public IEnumerable<INetworkObjectList> ObjectLists => _objectLists.AsEnumerable<INetworkObjectList>();

        protected BaseNetworkObjectListManager(TypeRegistry typeRegistry)
        {
            TypeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        }

        public INetworkObjectList FindListByName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _objectLists.Find(list => list.Name == name);
        }

        internal NetworkObjectList InternalFindListById(int id)
        {
            //Ids are just indices
            return _objectLists[id];
        }

        public INetworkObjectList FindListById(int id) => InternalFindListById(id);

        public NetworkObjectListListMetaDataList SerializeListMetaData()
        {
            var list = new NetworkObjectListListMetaDataList();

            foreach (var objectList in _objectLists)
            {
                list.MetaData.Add(new ListMetaData
                {
                    ListId = (uint)objectList.Id,
                    Name = objectList.Name
                });
            }

            return list;
        }

        public void DeserializeListMetaData(NetworkObjectListListMetaDataList list)
        {
            //TODO: maybe make this more robust
            if (_objectLists.Count > 0)
            {
                throw new InvalidOperationException("The object lists have already been created");
            }

            //Preallocate entries so we can insert properly
            for (var i = 0; i < list.MetaData.Count; ++i)
            {
                _objectLists.Add(null);
            }

            foreach (var metaData in list.MetaData)
            {
                if (_objectLists[(int)metaData.ListId] != null)
                {
                    throw new InvalidOperationException($"Received duplicate list metadata for list {metaData.Name} (id: {metaData.ListId})");
                }

                _objectLists[(int)metaData.ListId] = new NetworkObjectList(this, metaData.Name, (int)metaData.ListId);
            }
        }
    }
}
