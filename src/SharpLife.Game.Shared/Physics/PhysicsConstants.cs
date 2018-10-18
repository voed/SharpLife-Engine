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

namespace SharpLife.Game.Shared.Physics
{
    public static class PhysicsConstants
    {
        public const int MaxLeafs = 48;

        public const int MaxAreaNodes = 32;

        public const int MaxBoxSides = 6;

        public static readonly Vector3[] CurrentTable = new Vector3[]
        {
            new Vector3( 1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        public static class Hull1
        {
            public static readonly Vector3 ClipMins = new Vector3(-16, -16, -36);
            public static readonly Vector3 ClipMaxs = new Vector3(16, 16, 36);
        }

        public static class Hull2
        {
            public static readonly Vector3 ClipMins = new Vector3(-32, -32, -32);
            public static readonly Vector3 ClipMaxs = new Vector3(32, 32, 32);
        }

        public static class Hull3
        {
            public static readonly Vector3 ClipMins = new Vector3(-16, -16, -18);
            public static readonly Vector3 ClipMaxs = new Vector3(16, 16, 18);
        }
    }
}
