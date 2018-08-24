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

using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.FileFormats.BSP
{
    public class Model
    {
        public Vector3 Mins { get; set; }

        public Vector3 Maxs { get; set; }

        public Vector3 Origin { get; set; }

        public int[] HeadNodes { get; } = new int[BSPConstants.MaxHulls];

        public int NumVisLeaves { get; set; }       // not including the solid leaf 0

        public List<Face> Faces { get; set; }
    }
}
