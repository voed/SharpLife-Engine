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

using SharpLife.Engine.Shared.Models.BSP;
using System;

namespace SharpLife.Engine.Shared.Maps
{
    /// <summary>
    /// Read-only map info
    /// </summary>
    public sealed class MapInfo : IMapInfo
    {
        public string Name { get; }

        public string PreviousMapName { get; }

        public BSPModel Model { get; }

        public MapInfo(string name, string previousMapName, BSPModel model)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            PreviousMapName = previousMapName;
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }
}
