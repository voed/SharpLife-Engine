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

        public string Name { get; }

        public int Id { get; }

        internal IReadOnlyList<NetworkObject> InternalNetworkObjects => _networkObjects;

        internal NetworkObjectList(BaseNetworkObjectListManager listManager, string name, int id)
        {
            _listManager = listManager ?? throw new ArgumentNullException(nameof(listManager));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = id;
        }

        public NetworkObject InternalFindNetworkObjectForObject(INetworkable networkableObject)
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

        public INetworkObject FindNetworkObjectForObject(INetworkable networkableObject)
        {
            return InternalFindNetworkObjectForObject(networkableObject);
        }

        public NetworkObject InternalGetNetworkObjectById(int id)
        {
            return _networkObjects.Find(networkObject => networkObject.Handle.Id == id);
        }

        public INetworkObject GetNetworkObjectById(int id)
        {
            return InternalGetNetworkObjectById(id);
        }

        internal NetworkObject InternalCreateNetworkObject(INetworkable networkableObject)
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

            var networkObject = new NetworkObject(metaData, networkableObject);

            _networkObjects.Add(networkObject);

            return networkObject;
        }

        public INetworkObject CreateNetworkObject(INetworkable networkableObject)
        {
            return InternalCreateNetworkObject(networkableObject);
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
