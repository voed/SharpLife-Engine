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

using Google.Protobuf;
using Lidgren.Network;

namespace SharpLife.Networking.Shared.Communication.Messages
{
    /// <summary>
    /// Message handlers should implement this
    /// The same handler can implement different message handlers
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageReceiveHandler<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">The connection from which this message came</param>
        /// <param name="message"></param>
        void ReceiveMessage(NetConnection connection, TMessage message);
    }
}
