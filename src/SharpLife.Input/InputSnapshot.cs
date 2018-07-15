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
using System.Linq;
using System.Numerics;
using Veldrid;

namespace SharpLife.Input
{
    internal class InputSnapshot : IInputSnapshot
    {
        public List<KeyEvent> KeyEvents { get; private set; } = new List<KeyEvent>();

        public List<MouseEvent> MouseEvents { get; private set; } = new List<MouseEvent>();

        public List<char> KeyCharPresses { get; private set; } = new List<char>();

        IReadOnlyList<KeyEvent> Veldrid.InputSnapshot.KeyEvents => KeyEvents;

        IReadOnlyList<MouseEvent> Veldrid.InputSnapshot.MouseEvents => MouseEvents;

        IReadOnlyList<char> Veldrid.InputSnapshot.KeyCharPresses => KeyCharPresses;

        public Vector2 MousePosition { get; set; }

        public float WheelDelta { get; set; }

        public List<SDL.SDL_Event> Events { get; private set; } = new List<SDL.SDL_Event>();

        IReadOnlyList<SDL.SDL_Event> IInputSnapshot.Events => Events;

        public Dictionary<SDL.SDL_Keycode, KeyState> Keys { get; } = new Dictionary<SDL.SDL_Keycode, KeyState>();

        public HashSet<SDL.SDL_Keycode> KeysDownThisFrame { get; } = new HashSet<SDL.SDL_Keycode>();

        public Dictionary<MouseButton, MouseButtonState> MouseButtons { get; } = new Dictionary<MouseButton, MouseButtonState>();

        public HashSet<MouseButton> MouseButtonsDownThisFrame { get; } = new HashSet<MouseButton>();

        /// <summary>
        /// Creates an empty snapshot with all states set to their defaults
        /// </summary>
        public InputSnapshot()
        {
            foreach (SDL.SDL_Keycode code in Enum.GetValues(typeof(SDL.SDL_Keycode)))
            {
                Keys.Add(code, new KeyState(code));
            }

            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
            {
                MouseButtons.Add(button, new MouseButtonState(button));
            }
        }

        public IReadOnlyList<SDL.SDL_Event> GetEvents(SDL.SDL_EventType type)
        {
            return Events.Where(e => e.type == type).ToList();
        }

        public IReadOnlyList<SDL.SDL_Event> GetEvents(Func<SDL.SDL_Event, bool> filter)
        {
            return Events.Where(filter).ToList();
        }

        public bool IsKeyDown(SDL.SDL_Keycode key)
        {
            return Keys[key].Down;
        }

        public KeyState GetKeyState(SDL.SDL_Keycode key)
        {
            return Keys[key];
        }

        public bool IsKeyPressed(SDL.SDL_Keycode key)
        {
            return KeysDownThisFrame.Contains(key);
        }

        public bool IsMouseDown(MouseButton button)
        {
            return MouseButtons[button].Down;
        }

        public MouseButtonState GetMouseState(MouseButton button)
        {
            return MouseButtons[button];
        }

        public bool IsMousePressed(MouseButton button)
        {
            return MouseButtonsDownThisFrame.Contains(button);
        }

        public IInputSnapshot Clone()
        {
            var clone = new InputSnapshot();

            CopyTo(clone);

            return clone;
        }

        /// <summary>
        /// Clears all relative states
        /// </summary>
        public void Clear()
        {
            KeyEvents.Clear();
            MouseEvents.Clear();
            KeyCharPresses.Clear();

            WheelDelta = 0.0f;

            Events.Clear();

            KeysDownThisFrame.Clear();
            MouseButtonsDownThisFrame.Clear();
        }

        public void CopyTo(InputSnapshot other)
        {
            other.KeyEvents = KeyEvents.ToList();
            other.MouseEvents = MouseEvents.ToList();
            other.KeyCharPresses = KeyCharPresses.ToList();

            other.MousePosition = MousePosition;
            other.WheelDelta = WheelDelta;

            other.Events = Events.ToList();

            //Don't copy the dictionary every time, just update the states
            //This avoids allocating a lot of memory every frame

            foreach (var entry in Keys.Keys)
            {
                other.Keys[entry] = Keys[entry];
            }

            foreach (var entry in MouseButtons.Keys)
            {
                other.MouseButtons[entry] = MouseButtons[entry];
            }

            other.KeysDownThisFrame.Clear();
            other.MouseButtonsDownThisFrame.Clear();

            foreach (var entry in KeysDownThisFrame)
            {
                other.KeysDownThisFrame.Add(entry);
            }

            foreach (var entry in MouseButtonsDownThisFrame)
            {
                other.MouseButtonsDownThisFrame.Add(entry);
            }
        }
    }
}
