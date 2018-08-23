using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using System;
using System.Collections.Generic;

namespace SharpLife.Engine.Client.Networking
{
    internal sealed class ObjectListReceiverListener : IFrameListReceiverListener
    {
        private readonly Dictionary<int, IFrameListReceiverListener> _listeners = new Dictionary<int, IFrameListReceiverListener>();

        public void RegisterListener(int id, IFrameListReceiverListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _listeners.Add(id, listener);
        }

        public void OnBeginProcessList(INetworkObjectList networkObjectList)
        {
            if (_listeners.TryGetValue(networkObjectList.Id, out var listener))
            {
                listener.OnBeginProcessList(networkObjectList);
            }
        }

        public void OnEndProcessList(INetworkObjectList networkObjectList)
        {
            if (_listeners.TryGetValue(networkObjectList.Id, out var listener))
            {
                listener.OnEndProcessList(networkObjectList);
            }
        }

        public void OnNetworkObjectCreated(INetworkObjectList networkObjectList, INetworkObject networkObject, INetworkable networkableObject)
        {
            if (_listeners.TryGetValue(networkObjectList.Id, out var listener))
            {
                listener.OnNetworkObjectCreated(networkObjectList, networkObject, networkableObject);
            }
        }

        public void OnNetworkObjectDestroyed(INetworkObjectList networkObjectList, INetworkObject networkObject, INetworkable networkableObject)
        {
            if (_listeners.TryGetValue(networkObjectList.Id, out var listener))
            {
                listener.OnNetworkObjectDestroyed(networkObjectList, networkObject, networkableObject);
            }
        }

        public void OnBeginUpdateNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject)
        {
            if (_listeners.TryGetValue(networkObjectList.Id, out var listener))
            {
                listener.OnBeginUpdateNetworkObject(networkObjectList, networkObject);
            }
        }

        public void OnEndUpdateNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject)
        {
            if (_listeners.TryGetValue(networkObjectList.Id, out var listener))
            {
                listener.OnEndUpdateNetworkObject(networkObjectList, networkObject);
            }
        }
    }
}
