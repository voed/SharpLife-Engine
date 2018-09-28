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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Models;
using System.Numerics;

namespace SharpLife.Game.Client.Renderer.Shared.Models
{
    public struct SharedModelRenderData
    {
        public uint Index;

        public Vector3 Origin;
        public Vector3 Angles;
        public Vector3 Scale;

        public RenderFX RenderFX;
        public RenderMode RenderMode;
        public int RenderAmount;
        public Vector3 RenderColor;

        public EffectsFlags Effects;
    }
}
