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

namespace SharpLife.Game.Client.Renderer.Shared
{
    /// <summary>
    /// A renderable object can specify which render passes to be part of with using these flags
    /// </summary>
    [Flags]
    public enum RenderPasses : int
    {
        None = 0,
        Standard = 1 << 0,
        AlphaBlend = 1 << 1,
        Overlay = 1 << 2,
        SwapchainOutput = 1 << 3
    }
}
