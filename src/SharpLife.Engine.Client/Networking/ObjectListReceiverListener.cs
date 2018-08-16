using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;

namespace SharpLife.Engine.Client.Networking
{
    internal sealed class ObjectListReceiverListener : IFrameListReceiverListener
    {
        public void OnBeginProcessList(INetworkObjectList networkObjectList)
        {
        }

        public void OnEndProcessList(INetworkObjectList networkObjectList)
        {
        }

        public void OnNetworkObjectCreated(INetworkObjectList networkObjectList, INetworkObject networkObject, object networkableObject)
        {
        }

        public void OnNetworkObjectDestroyed(INetworkObjectList networkObjectList, INetworkObject networkObject, object networkableObject)
        {
        }

        public void OnBeginUpdateNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject)
        {
        }

        public void OnEndUpdateNetworkObject(INetworkObjectList networkObjectList, INetworkObject networkObject)
        {
        }
    }
}
