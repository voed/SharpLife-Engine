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

using Google.Protobuf.Reflection;
using SharpLife.Networking.Shared.Communication.BinaryData;
using SharpLife.Networking.Shared.Messages.BinaryData;
using SharpLife.Networking.Shared.Messages.Client;
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using SharpLife.Networking.Shared.Messages.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;
using System;
using System.Collections.Generic;

namespace SharpLife.Networking.Shared
{
    /// <summary>
    /// String message identifiers and protobuf messages used by networking code
    /// </summary>
    public static class NetMessages
    {
        public const string ClientDisconnectMessage = "dropclient";
        public const string ClientShutdownMessage = "Client shutting down";

        public const string ServerShutdownUnknown = "Server shutting down for unknown reason";

        public const string ServerShutdownMessage = "Server shutting down";

        public const string ServerChangeLevel = "Server changing level";

        public const string ServerClientDeniedInactive = "Server is inactive.";

        public const string ServerClientDeniedNoFreeSlots = "Server is full.";

        public const string ServerClientDeniedProtocolVersionOlder = "You are running an older version";

        public const string ServerClientDeniedProtocolVersionNewer = "You are running a newer version";

        /// <summary>
        /// List of client-to-server messages used by the engine
        /// The order of these messages is important; the client maps messages to their index, the server maps indices to messages
        /// If you change this in any way, update <see cref="NetConstants.ProtocolVersion"/>
        /// </summary>
        public static IReadOnlyList<MessageDescriptor> ClientToServerMessages { get; } = new List<MessageDescriptor>
        {
            //ClientUserInfo message is not included in this since it's the first message that gets sent
            NewConnection.Descriptor,
            SendResources.Descriptor,
        };

        /// <summary>
        /// List of server-to-client messages used by the engine
        /// The order of these messages is important; the server maps messages to their index, the client maps indices to messages
        /// If you change this in any way, update <see cref="NetConstants.ProtocolVersion"/>
        /// </summary>
        public static IReadOnlyList<MessageDescriptor> ServerToClientMessages { get; } = new List<MessageDescriptor>
        {
            ConnectAcknowledgement.Descriptor,
            ServerInfo.Descriptor,
            Print.Descriptor,
            BinaryMetaData.Descriptor,
            NetworkStringListFullUpdate.Descriptor,
            NetworkStringListUpdate.Descriptor,
            NetworkObjectListFrameListUpdate.Descriptor,
            NetworkObjectListObjectMetaDataList.Descriptor,
            NetworkObjectListListMetaDataList.Descriptor,
        };

        /// <summary>
        /// Registers Protobuf messages used by the engine for use as binary data in networking subsystems
        /// </summary>
        /// <param name="dataSetBuilder"></param>
        public static void RegisterEngineBinaryDataTypes(IBinaryDataSetBuilder dataSetBuilder)
        {
            if (dataSetBuilder == null)
            {
                throw new ArgumentNullException(nameof(dataSetBuilder));
            }

            dataSetBuilder.Add(ModelPrecacheData.Descriptor);
        }
    }
}
