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

using SharpLife.Engine.Shared.API.Game.Shared;
using SharpLife.Models.BSP;

namespace SharpLife.Game.Shared.Bridge
{
    /// <summary>
    /// Represents the bridge between client and server
    /// Used to share state and utility objects
    /// </summary>
    public interface IGameBridge : IBridge
    {
        IBridgeDataReceiver DataReceiver { get; }

        BSPModelUtils ModelUtils { get; }
    }
}
