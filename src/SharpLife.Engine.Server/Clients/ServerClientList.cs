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

using SharpLife.CommandSystem.Commands;
using SharpLife.Engine.Shared.API.Engine.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SharpLife.Engine.Server.Clients
{
    /// <summary>
    /// Maintains the list of clients and provides common operations on them
    /// Enumerable enumerates all active clients
    /// </summary>
    public sealed class ServerClientList : IEnumerable<ServerClient>, IServerClients
    {
        private readonly List<ServerClient> _clients;

        public int MaxClients => _maxPlayers.Integer;

        /// <summary>
        /// The number of clients on the server
        /// </summary>
        public int Count { get; private set; }

        private readonly IVariable _maxPlayers;

        public ServerClientList(int maxClients, IVariable maxPlayers)
        {
            _clients = new List<ServerClient>(maxClients);
            _maxPlayers = maxPlayers ?? throw new ArgumentNullException(nameof(maxPlayers));

            //Add null entries for each slot
            //This simplifies a lot of logic
            foreach (var i in Enumerable.Range(0, maxClients))
            {
                _clients.Add(null);
            }
        }

        public int FindEmptySlot()
        {
            for (var i = 0; i < _maxPlayers.Integer; ++i)
            {
                if (_clients[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public void AddClientToSlot(ServerClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var slot = client.Index;

            if (slot < 0 || slot >= _clients.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Client slot index is out of range");
            }

            if (_clients[slot] != null)
            {
                throw new ArgumentException($"Client slot {slot} is already in use");
            }

            _clients[slot] = client;

            ++Count;
        }

        public void RemoveClient(ServerClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var index = client.Index;

            if (index < 0 || index >= _clients.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Client slot index is out of range");
            }

            if (_clients[index] != client)
            {
                throw new InvalidOperationException("Client does not match client at slot");
            }

            _clients[index] = null;

            --Count;
        }

        public ServerClient FindClientByEndPoint(IPEndPoint endPoint, bool throwOnNotFound = true)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            for (var i = 0; i < _maxPlayers.Integer; ++i)
            {
                if (_clients[i]?.RemoteEndPoint?.Equals(endPoint) == true)
                {
                    return _clients[i];
                }
            }

            if (throwOnNotFound)
            {
                throw new InvalidOperationException($"Couldn't find client for end point {endPoint}");
            }

            return null;
        }

        public IEnumerator<ServerClient> GetEnumerator()
        {
            for (var i = 0; i < _clients.Count; ++i)
            {
                if (_clients[i] != null)
                {
                    yield return _clients[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
