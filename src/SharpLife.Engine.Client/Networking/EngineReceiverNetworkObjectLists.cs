using SharpLife.Engine.API.Engine.Shared;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using System;

namespace SharpLife.Engine.Client.Networking
{
    internal sealed class EngineReceiverNetworkObjectLists : IEngineNetworkObjectLists, IDisposable
    {
        private readonly BaseNetworkObjectListManager _objectListManager;

        private bool _disposed;

        internal EngineReceiverNetworkObjectLists(BaseNetworkObjectListManager objectListManager)
        {
            _objectListManager = objectListManager ?? throw new ArgumentNullException(nameof(objectListManager));
        }

        public INetworkObjectList CreateList(string name)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EngineReceiverNetworkObjectLists));
            }

            //Receivers rely on the transmitter to provide the lists they need
            var objectList = _objectListManager.FindListByName(name);

            if (objectList == null)
            {
                throw new InvalidOperationException($"Object list {name} does not exist");
            }

            return objectList;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
