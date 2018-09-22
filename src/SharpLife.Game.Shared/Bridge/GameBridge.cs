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

using SharpLife.Engine.Shared;
using SharpLife.Models.BSP;
using System;

namespace SharpLife.Game.Shared.Bridge
{
    public sealed class GameBridge : IGameBridge
    {
        public IBridgeDataReceiver DataReceiver { get; }

        public BSPModelUtils ModelUtils { get; }

        public GameBridge(IBridgeDataReceiver dataReceiver, BSPModelUtils modelUtils)
        {
            DataReceiver = dataReceiver;
            ModelUtils = modelUtils ?? throw new ArgumentNullException(nameof(modelUtils));
        }

        public static GameBridge CreateBridge(IBridgeDataReceiver dataReceiver)
        {
            return new GameBridge(dataReceiver, new BSPModelUtils(Framework.BSPModelNamePrefix, Framework.Directory.Maps, Framework.Extension.BSP));
        }
    }
}
