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

using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists
{
    internal sealed class NetworkObjectList : INetworkObjectList
    {
        private readonly BaseNetworkObjectListManager _listManager;

        private readonly List<NetworkObject> _networkObjects = new List<NetworkObject>();

        private int _nextId;

        private readonly List<int> _freeIds = new List<int>();

        public string Name { get; }

        public int Id { get; }

        internal IReadOnlyList<NetworkObject> InternalNetworkObjects => _networkObjects;

        internal NetworkObjectList(BaseNetworkObjectListManager listManager, string name, int id)
        {
            _listManager = listManager ?? throw new ArgumentNullException(nameof(listManager));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = id;
        }

        public NetworkObject InternalFindNetworkObjectForObject(object networkableObject)
        {
            if (networkableObject == null)
            {
                throw new ArgumentNullException(nameof(networkableObject));
            }

            foreach (var networkObject in _networkObjects)
            {
                if (ReferenceEquals(networkObject.Instance, networkableObject))
                {
                    return networkObject;
                }
            }

            return null;
        }

        public INetworkObject FindNetworkObjectForObject(object networkableObject)
        {
            return InternalFindNetworkObjectForObject(networkableObject);
        }

        public NetworkObject InternalGetNetworkObjectById(int id)
        {
            return _networkObjects.Find(networkObject => networkObject.Id == id);
        }

        public INetworkObject GetNetworkObjectById(int id)
        {
            return InternalGetNetworkObjectById(id);
        }

        private int GetFreeId()
        {
            int id;

            if (_freeIds.Count > 0)
            {
                //TODO: could take the lowest id at all times to reduce varint size
                //TODO: or have user hint at how often object updates to optimize id usage
                id = _freeIds[_freeIds.Count - 1];
                _freeIds.RemoveAt(_freeIds.Count - 1);
            }
            else
            {
                //If you absolutely need more, convert everything to uint64
                if (_nextId == int.MaxValue)
                {
                    throw new InvalidOperationException($"Cannot create more network objects, ran out of IDs (max: {int.MaxValue})");
                }

                id = _nextId++;
            }

            return id;
        }

        internal NetworkObject InternalCreateNetworkObject(object networkableObject, int? objectId)
        {
            if (networkableObject == null)
            {
                throw new ArgumentNullException(nameof(networkableObject));
            }

            if (InternalFindNetworkObjectForObject(networkableObject) != null)
            {
                throw new InvalidOperationException($"Object \"{networkableObject}\" already has a network object associated with it");
            }

            var type = networkableObject.GetType();

            var metaData = _listManager.TypeRegistry.FindMetaDataByType(type);

            if (metaData == null)
            {
                throw new InvalidOperationException($"Type {type.FullName} has not been registered to be networked");
            }

            if (objectId.HasValue && GetNetworkObjectById(objectId.Value) != null)
            {
                throw new InvalidOperationException($"There is already an object with id {objectId.Value}");
            }

            var id = objectId ?? GetFreeId();

            var networkObject = new NetworkObject(id, metaData, networkableObject);

            _networkObjects.Add(networkObject);

            return networkObject;
        }

        public INetworkObject CreateNetworkObject<TNetworkable>(TNetworkable networkableObject)
            where TNetworkable : class
        {
            return InternalCreateNetworkObject(networkableObject, null);
        }

        internal void InternalDestroyNetworkObject(NetworkObject networkObject)
        {
            if (networkObject == null)
            {
                throw new ArgumentNullException(nameof(networkObject));
            }

            if (!_networkObjects.Contains(networkObject))
            {
                throw new InvalidOperationException("Network object is not managed by this list");
            }

            networkObject.Destroyed = true;
        }

        public void DestroyNetworkObject(INetworkObject networkObject)
        {
            InternalDestroyNetworkObject((NetworkObject)networkObject);
        }

        internal void PostFramesCreated()
        {
            for (var i = 0; i < _networkObjects.Count;)
            {
                if (_networkObjects[i].Destroyed)
                {
                    _freeIds.Add(_networkObjects[i].Id);
                    _networkObjects.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }

        internal void PostFramesReceived()
        {
            for (var i = 0; i < _networkObjects.Count;)
            {
                if (_networkObjects[i].Destroyed)
                {
                    _freeIds.Add(_networkObjects[i].Id);
                    _networkObjects.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }
    }
}
