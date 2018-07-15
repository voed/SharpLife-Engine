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

using SDL2;

namespace SharpLife.Input
{
    /// <summary>
    /// Contains the state of a key
    /// </summary>
    public struct KeyState
    {
        public SDL.SDL_Keycode Key { get; }

        public bool Down { get; set; }

        public SDL.SDL_Keymod Modifiers { get; set; }

        /// <summary>
        /// The last time the state of this key changed
        /// </summary>
        public uint ChangeTimestamp { get; set; }

        public KeyState(SDL.SDL_Keycode key)
        {
            Key = key;
            Down = false;
            Modifiers = SDL.SDL_Keymod.KMOD_NONE;
            ChangeTimestamp = 0;
        }
    }
}
