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

using SharpLife.Engine.API.Engine.Server;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission;
using System;

namespace SharpLife.Engine.Server.Networking
{
    internal sealed class EngineTransmitterNetworkObjectLists : IServerNetworkObjectLists, IDisposable
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
