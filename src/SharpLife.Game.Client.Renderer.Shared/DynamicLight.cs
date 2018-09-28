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

using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace SharpLife.Game.Client.Renderer.Shared
{
    public struct DynamicLight
    {
        public Vector3 Origin;
        public float Radius;
        public Rgb24 Color;

        public float Die;              // stop lighting after this time
        public float Decay;                // drop this each second
        public float MinLight;         // don't add when contributing less

        public uint Key;
    }
}
