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

namespace SharpLife.Game.Shared.Audio
{
    public static class Attenuation
    {
        /// <summary>
        /// Disables attenuation. The sound will play everywhere
        /// </summary>
        public const float None = 0;

        public const float Normal = 0.8f;

        public const float Idle = 2.0f;

        public const float Static = 1.25f;
    }
}
