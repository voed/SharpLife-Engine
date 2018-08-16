using SharpLife.Engine.API.Engine.Shared;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission;
using System;

namespace SharpLife.Engine.Server.Networking
{
    internal sealed class EngineTransmitterNetworkObjectLists : IEngineNetworkObjectLists, IDisposable
    {
        private readonly NetworkObjectListTransmitter _objectListManager;

        private bool _disposed;

        internal EngineTransmitterNetworkObjectLists(NetworkObjectListTransmitter objectListManager)
        {
            _objectListManager = objectListManager ?? throw new ArgumentNullException(nameof(objectListManager));
        }

        public INetworkObjectList CreateList(string name)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EngineTransmitterNetworkObjectLists));
            }

            return _objectListManager.CreateList(name);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
