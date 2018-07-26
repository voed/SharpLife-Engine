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

using Lidgren.Network;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Clients
{
    /// <summary>
    /// Represents a single client on a server
    /// </summary>
    internal sealed class ServerClient
    {
        public NetConnection Connection { get; }

        public IPEndPoint RemoteEndPoint => Connection?.RemoteEndPoint;

        /// <summary>
        /// This client's index
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Whether this is a fake client (server only bot)
        /// </summary>
        public bool IsFakeClient { get; }

        public bool Connected { get; set; }

        public string Name { get; set; }

        private ServerClient(NetConnection connection, int index, string name)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Index = index;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Creates a fake client
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        private ServerClient(int index, string name)
        {
            IsFakeClient = true;
            Index = index;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static ServerClient CreateClient(NetConnection connection, int index, string name)
        {
            return new ServerClient(connection, index, name);
        }

        /// <summary>
        /// Creates a fake client that will behave like a client on the server, but has no network connection
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ServerClient CreateFakeClient(int index, string name)
        {
            return new ServerClient(index, name);
        }

        public void Disconnect(string reason)
        {
            if (reason == null)
            {
                throw new ArgumentNullException(nameof(reason));
            }

            if (!IsFakeClient)
            {
                Connection.Disconnect(reason);
            }
        }
    }
}
