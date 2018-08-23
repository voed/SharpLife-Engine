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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception
{
    //Not quite a builder, but the user will treat it as such
    public sealed class NetworkObjectListReceiverBuilder : INetworkObjectListReceiverBuilder, IDisposable
    {
        private readonly BaseNetworkObjectListManager _objectListManager;

        private readonly Action<INetworkObjectList, IFrameListReceiverListener> _callback;

        private bool _disposed;

        /// <summary>
        /// Creates a new builder
        /// </summary>
        /// <param name="objectListManager"></param>
        /// <param name="callback">Callback to invoke for each list that is created</param>
        public NetworkObjectListReceiverBuilder(BaseNetworkObjectListManager objectListManager, Action<INetworkObjectList, IFrameListReceiverListener> callback)
        {
            _objectListManager = objectListManager ?? throw new ArgumentNullException(nameof(objectListManager));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public INetworkObjectList CreateList(string name, IFrameListReceiverListener listener)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NetworkObjectListReceiverBuilder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            //Receivers rely on the transmitter to provide the lists they need
            var objectList = _objectListManager.FindListByName(name);

            if (objectList == null)
            {
                throw new InvalidOperationException($"Object list {name} does not exist");
            }

            _callback(objectList, listener);

            return objectList;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
