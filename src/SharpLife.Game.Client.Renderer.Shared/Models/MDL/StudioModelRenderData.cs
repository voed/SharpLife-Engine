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

using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Models.MDL.Rendering;

namespace SharpLife.Game.Client.Renderer.Shared.Models.MDL
{
    public unsafe struct StudioModelRenderData
    {
        public StudioModel Model;

        public SharedModelRenderData Shared;

        public uint Sequence;

        public float Frame;

        public int Body;

        public int Skin;

        public BoneData BoneData;
    }
}
