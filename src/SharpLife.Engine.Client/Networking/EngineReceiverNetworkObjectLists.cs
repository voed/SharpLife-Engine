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

using SharpLife.Engine.API.Engine.Client;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Reception;
using System;

namespace SharpLife.Engine.Client.Networking
{
    internal sealed class EngineReceiverNetworkObjectLists : IClientNetworkObjectLists, IDisposable
    {
        private readonly BaseNetworkObjectListManager _objectListManager;

        private readonly ObjectListReceiverListener _mainListener;

        private bool _disposed;

        internal EngineReceiverNetworkObjectLists(BaseNetworkObjectListManager objectListManager, ObjectListReceiverListener mainListener)
        {
            _objectListManager = objectListManager ?? throw new ArgumentNullException(nameof(objectListManager));
            _mainListener = mainListener ?? throw new ArgumentNullException(nameof(mainListener));
        }

        public INetworkObjectList CreateList(string name, IFrameListReceiverListener listener)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EngineReceiverNetworkObjectLists));
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

            _mainListener.RegisterListener(objectList.Id, listener);

            return objectList;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
