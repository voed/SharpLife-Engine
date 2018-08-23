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
using SharpLife.Networking.Shared.Communication.Messages;
using SharpLife.Networking.Shared.Messages.Server;

namespace SharpLife.Engine.Client.Host
{
    public partial class EngineClientHost :
        IMessageReceiveHandler<ServerInfo>,
        IMessageReceiveHandler<Print>
    {
        private void RegisterMessageHandlers(MessagesReceiveHandler receiveHandler)
        {
            receiveHandler.RegisterHandler<ServerInfo>(this);
            receiveHandler.RegisterHandler<Print>(this);
        }

        public void ReceiveMessage(NetConnection connection, ServerInfo message)
        {
            //TODO: implement
            //TODO: when finished, move this into NetworkClient if possible
            //TODO: this is temporary. map loading occurs later on, so most of this will disappear
            if (!_engine.IsDedicatedServer)
            {
                //Load the BSP file
                if (!_engine.MapManager.LoadMap(message.MapFileName))
                {
                    _logger.Error($"Couldn't load \"{message.MapFileName}\"");
                    Disconnect(true);
                    return;
                }
            }

            _netClient.OnNewMapStarted();

            _renderer.LoadBSP(_engine.MapManager.BSPFile);

            _game.MapLoadBegin(_engine.MapManager.MapName, _engine.MapManager.BSPFile.Entities);

            _game.MapLoadFinished();

            _netClient.RequestResources();
        }

        public void ReceiveMessage(NetConnection connection, Print message)
        {
            _logger.Information(message.MessageContents);
        }
    }
}
