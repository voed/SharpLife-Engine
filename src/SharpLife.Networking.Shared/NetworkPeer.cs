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

namespace SharpLife.Networking.Shared
{
    /// <summary>
    /// Base class for network handlers
    /// </summary>
    public abstract class NetworkPeer
    {
        protected abstract NetPeer Peer { get; }

        public void Start()
        {
            Peer.Start();
        }

        public void Shutdown(string bye)
        {
            Peer.Shutdown(bye);
        }

        public void ReadPackets()
        {
            NetIncomingMessage im;

            while ((im = Peer.ReadMessage()) != null)
            {
                HandlePacket(im);

                Peer.Recycle(im);
            }
        }

        protected abstract void HandlePacket(NetIncomingMessage message);

        public void FlushOutgoingPackets()
        {
            Peer.FlushSendQueue();
        }

        public NetOutgoingMessage CreatePacket()
        {
            return Peer.CreateMessage();
        }

        public void SendPacket(NetOutgoingMessage message, NetConnection recipient, NetDeliveryMethod method)
        {
            Peer.SendMessage(message, recipient, method);
        }
    }
}
