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
using System;
using System.Collections.Generic;
using Veldrid;

namespace SharpLife.Input
{
    /// <summary>
    /// Provides access to a snapshot of user input
    /// </summary>
    public interface IInputSnapshot : Veldrid.InputSnapshot
    {
        /// <summary>
        /// The list of all events contained in this snapshot
        /// </summary>
        IReadOnlyList<SDL.SDL_Event> Events { get; }

        /// <summary>
        /// Gets a list of all events that are of the given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IReadOnlyList<SDL.SDL_Event> GetEvents(SDL.SDL_EventType type);

        /// <summary>
        /// Gets a list of all events that pass the given filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        IReadOnlyList<SDL.SDL_Event> GetEvents(Func<SDL.SDL_Event, bool> filter);

        /// <summary>
        /// Gets whether a key is down
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsKeyDown(SDL.SDL_Keycode key);

        /// <summary>
        /// Gets the current state of a key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        KeyState GetKeyState(SDL.SDL_Keycode key);

        /// <summary>
        /// Gets whether a key was pressed this frame
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsKeyPressed(SDL.SDL_Keycode key);

        /// <summary>
        /// Gets the current state of a mouse button
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        MouseButtonState GetMouseState(MouseButton button);

        /// <summary>
        /// Gets whether a mouse button was pressed this frame
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        bool IsMousePressed(MouseButton button);

        /// <summary>
        /// Creates a copy of this snapshot
        /// </summary>
        /// <returns></returns>
        IInputSnapshot Clone();
    }
}
