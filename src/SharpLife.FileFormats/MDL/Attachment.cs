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

using System.Numerics;

namespace SharpLife.FileFormats.MDL
{
    public class Attachment
    {
        public string Name { get; set; }

        public int Type { get; set; }

        public Bone Bone { get; set; }

        public Vector3 Origin { get; set; }

        public Vector3 Vector0 { get; set; }
        public Vector3 Vector1 { get; set; }
        public Vector3 Vector2 { get; set; }
    }
}
