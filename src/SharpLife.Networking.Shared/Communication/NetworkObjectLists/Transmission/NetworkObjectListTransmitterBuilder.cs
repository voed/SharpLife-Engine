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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.Transmission
{
    //Not quite a builder, but the user will treat it as such
    public sealed class NetworkObjectListTransmitterBuilder : INetworkObjectListTransmitterBuilder, IDisposable
    {
        private readonly NetworkObjectListTransmitter _objectListManager;

        private bool _disposed;

        public NetworkObjectListTransmitterBuilder(NetworkObjectListTransmitter objectListManager)
        {
            _objectListManager = objectListManager ?? throw new ArgumentNullException(nameof(objectListManager));
        }

        public INetworkObjectList CreateList(string name)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NetworkObjectListTransmitterBuilder));
            }

            return _objectListManager.CreateList(name);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
