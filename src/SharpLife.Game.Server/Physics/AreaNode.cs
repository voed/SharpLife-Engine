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

using SharpLife.Game.Server.Entities;
using System.Collections.Generic;

namespace SharpLife.Game.Server.Physics
{
    public sealed class AreaNode
    {
        public int Axis;
        public float Distance;
        public readonly AreaNode[] Children = new AreaNode[2];
        public readonly List<BaseEntity> Triggers = new List<BaseEntity>();
        public readonly List<BaseEntity> Solids = new List<BaseEntity>();
    }
}
