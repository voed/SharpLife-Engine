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

using SharpLife.Engine.Server.Clients;
using System;
using System.Net;

namespace SharpLife.Engine.Server.Host
{
    public partial class EngineServerHost
    {
        public ServerClientList ClientList { get; }

        public ServerClient FindClient(IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            var client = ClientList.FindClientByEndPoint(endPoint, false);

            if (client != null)
            {
                return client;
            }

            _logger.Warning($"Client with IP {endPoint} has no associated client instance");

            return null;
        }

        private void DropClient(ServerClient client, string reason)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (reason == null)
            {
                throw new ArgumentNullException(nameof(reason));
            }

            //TODO: notify game

            _logger.Information($"Dropped {client.Name} from server\nReason:  {reason}");

            client.Disconnect(reason, _netServer.ObjectListTransmitter);
        }
    }
}
