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
using System.Numerics;
using System.Text;
using Veldrid;

namespace SharpLife.Input
{
    /// <summary>
    /// Manages user inputs and tracks changes between frames
    /// Provides a snapshot of the current frame
    /// </summary>
    public class InputSystem : IInputSystem
    {
        private readonly InputSnapshot _privateSnapshot = new InputSnapshot();

        private readonly InputSnapshot _publicSnapshot = new InputSnapshot();

        public IInputSnapshot Snapshot => _publicSnapshot;

        public void ProcessEvents(int milliSeconds)
        {
            SDL.SDL_PumpEvents();

            if (SDL.SDL_WaitEventTimeout(out var sdlEvent, milliSeconds) > 0)
            {
                //Process all events
                do
                {
                    _privateSnapshot.Events.Add(sdlEvent);
                }
                while (SDL.SDL_PollEvent(out sdlEvent) > 0);
            }

            for (int i = 0; i < _privateSnapshot.Events.Count; ++i)
            {
                //Preprocess keyboard and mouse input to provide more information
                ProcessEvent(ref sdlEvent);
            }

            _privateSnapshot.CopyTo(_publicSnapshot);
            _privateSnapshot.Clear();
        }

        private unsafe void ProcessEvent(ref SDL.SDL_Event sdlEvent)
        {
            switch (sdlEvent.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    {
                        var down = sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN;

                        var key = sdlEvent.key.keysym.sym;

                        var state = _privateSnapshot.Keys[key];

                        state.Down = down;
                        state.Modifiers = sdlEvent.key.keysym.mod;
                        state.ChangeTimestamp = sdlEvent.key.timestamp;

                        _privateSnapshot.Keys[key] = state;

                        if (down)
                        {
                            _privateSnapshot.KeysDownThisFrame.Add(key);
                        }
                        else
                        {
                            _privateSnapshot.KeysDownThisFrame.Remove(key);
                        }

                        _privateSnapshot.KeyEvents.Add(new KeyEvent(MapKey(ref sdlEvent.key.keysym), sdlEvent.key.state == 1, MapModifierKeys(sdlEvent.key.keysym.mod)));
                        break;
                    }

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    {
                        var down = sdlEvent.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN;

                        var button = MapMouseButton(sdlEvent.button.button);

                        var state = _privateSnapshot.MouseButtons[button];

                        state.Down = down;
                        state.ChangeTimestamp = sdlEvent.button.timestamp;

                        _privateSnapshot.MouseButtons[button] = state;

                        if (down)
                        {
                            _privateSnapshot.MouseButtonsDownThisFrame.Add(button);
                        }
                        else
                        {
                            _privateSnapshot.MouseButtonsDownThisFrame.Remove(button);
                        }

                        _privateSnapshot.MouseEvents.Add(new MouseEvent((MouseButton)sdlEvent.button.button, sdlEvent.button.state == 1));
                        break;
                    }

                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    {
                        fixed (byte* text = sdlEvent.text.text)
                        {
                            uint byteCount = 0;
                            // Loop until the null terminator is found or the max size is reached.
                            while (byteCount < SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE && text[byteCount++] != 0)
                            {
                            }

                            if (byteCount > 1)
                            {
                                // We don't want the null terminator.
                                --byteCount;
                                int charCount = Encoding.UTF8.GetCharCount(text, (int)byteCount);
                                char* charsPtr = stackalloc char[charCount];
                                Encoding.UTF8.GetChars(text, (int)byteCount, charsPtr, charCount);
                                for (int i = 0; i < charCount; i++)
                                {
                                    _privateSnapshot.KeyCharPresses.Add(charsPtr[i]);
                                }
                            }
                        }
                        break;
                    }

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    {
                        _privateSnapshot.MousePosition = new Vector2(sdlEvent.motion.x, sdlEvent.motion.y);
                        break;
                    }

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    {
                        var value = sdlEvent.wheel.y;

                        if ((SDL.SDL_MouseWheelDirection)sdlEvent.wheel.direction == SDL.SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED)
                        {
                            value *= -1;
                        }

                        _privateSnapshot.WheelDelta += value;
                        break;
                    }
            }
        }

        private static Key MapKey(ref SDL.SDL_Keysym keysym)
        {
            switch (keysym.scancode)
            {
                case SDL.SDL_Scancode.SDL_SCANCODE_A:
                    return Key.A;
                case SDL.SDL_Scancode.SDL_SCANCODE_B:
                    return Key.B;
                case SDL.SDL_Scancode.SDL_SCANCODE_C:
                    return Key.C;
                case SDL.SDL_Scancode.SDL_SCANCODE_D:
                    return Key.D;
                case SDL.SDL_Scancode.SDL_SCANCODE_E:
                    return Key.E;
                case SDL.SDL_Scancode.SDL_SCANCODE_F:
                    return Key.F;
                case SDL.SDL_Scancode.SDL_SCANCODE_G:
                    return Key.G;
                case SDL.SDL_Scancode.SDL_SCANCODE_H:
                    return Key.H;
                case SDL.SDL_Scancode.SDL_SCANCODE_I:
                    return Key.I;
                case SDL.SDL_Scancode.SDL_SCANCODE_J:
                    return Key.J;
                case SDL.SDL_Scancode.SDL_SCANCODE_K:
                    return Key.K;
                case SDL.SDL_Scancode.SDL_SCANCODE_L:
                    return Key.L;
                case SDL.SDL_Scancode.SDL_SCANCODE_M:
                    return Key.M;
                case SDL.SDL_Scancode.SDL_SCANCODE_N:
                    return Key.N;
                case SDL.SDL_Scancode.SDL_SCANCODE_O:
                    return Key.O;
                case SDL.SDL_Scancode.SDL_SCANCODE_P:
                    return Key.P;
                case SDL.SDL_Scancode.SDL_SCANCODE_Q:
                    return Key.Q;
                case SDL.SDL_Scancode.SDL_SCANCODE_R:
                    return Key.R;
                case SDL.SDL_Scancode.SDL_SCANCODE_S:
                    return Key.S;
                case SDL.SDL_Scancode.SDL_SCANCODE_T:
                    return Key.T;
                case SDL.SDL_Scancode.SDL_SCANCODE_U:
                    return Key.U;
                case SDL.SDL_Scancode.SDL_SCANCODE_V:
                    return Key.V;
                case SDL.SDL_Scancode.SDL_SCANCODE_W:
                    return Key.W;
                case SDL.SDL_Scancode.SDL_SCANCODE_X:
                    return Key.X;
                case SDL.SDL_Scancode.SDL_SCANCODE_Y:
                    return Key.Y;
                case SDL.SDL_Scancode.SDL_SCANCODE_Z:
                    return Key.Z;
                case SDL.SDL_Scancode.SDL_SCANCODE_1:
                    return Key.Number1;
                case SDL.SDL_Scancode.SDL_SCANCODE_2:
                    return Key.Number2;
                case SDL.SDL_Scancode.SDL_SCANCODE_3:
                    return Key.Number3;
                case SDL.SDL_Scancode.SDL_SCANCODE_4:
                    return Key.Number4;
                case SDL.SDL_Scancode.SDL_SCANCODE_5:
                    return Key.Number5;
                case SDL.SDL_Scancode.SDL_SCANCODE_6:
                    return Key.Number6;
                case SDL.SDL_Scancode.SDL_SCANCODE_7:
                    return Key.Number7;
                case SDL.SDL_Scancode.SDL_SCANCODE_8:
                    return Key.Number8;
                case SDL.SDL_Scancode.SDL_SCANCODE_9:
                    return Key.Number9;
                case SDL.SDL_Scancode.SDL_SCANCODE_0:
                    return Key.Number0;
                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN:
                    return Key.Enter;
                case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE:
                    return Key.Escape;
                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE:
                    return Key.BackSpace;
                case SDL.SDL_Scancode.SDL_SCANCODE_TAB:
                    return Key.Tab;
                case SDL.SDL_Scancode.SDL_SCANCODE_SPACE:
                    return Key.Space;
                case SDL.SDL_Scancode.SDL_SCANCODE_MINUS:
                    return Key.Minus;
                case SDL.SDL_Scancode.SDL_SCANCODE_EQUALS:
                    return Key.Plus;
                case SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET:
                    return Key.BracketLeft;
                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET:
                    return Key.BracketRight;
                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH:
                    return Key.BackSlash;
                case SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON:
                    return Key.Semicolon;
                case SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE:
                    return Key.Quote;
                case SDL.SDL_Scancode.SDL_SCANCODE_GRAVE:
                    return Key.Grave;
                case SDL.SDL_Scancode.SDL_SCANCODE_COMMA:
                    return Key.Comma;
                case SDL.SDL_Scancode.SDL_SCANCODE_PERIOD:
                    return Key.Period;
                case SDL.SDL_Scancode.SDL_SCANCODE_SLASH:
                    return Key.Slash;
                case SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK:
                    return Key.CapsLock;
                case SDL.SDL_Scancode.SDL_SCANCODE_F1:
                    return Key.F1;
                case SDL.SDL_Scancode.SDL_SCANCODE_F2:
                    return Key.F2;
                case SDL.SDL_Scancode.SDL_SCANCODE_F3:
                    return Key.F3;
                case SDL.SDL_Scancode.SDL_SCANCODE_F4:
                    return Key.F4;
                case SDL.SDL_Scancode.SDL_SCANCODE_F5:
                    return Key.F5;
                case SDL.SDL_Scancode.SDL_SCANCODE_F6:
                    return Key.F6;
                case SDL.SDL_Scancode.SDL_SCANCODE_F7:
                    return Key.F7;
                case SDL.SDL_Scancode.SDL_SCANCODE_F8:
                    return Key.F8;
                case SDL.SDL_Scancode.SDL_SCANCODE_F9:
                    return Key.F9;
                case SDL.SDL_Scancode.SDL_SCANCODE_F10:
                    return Key.F10;
                case SDL.SDL_Scancode.SDL_SCANCODE_F11:
                    return Key.F11;
                case SDL.SDL_Scancode.SDL_SCANCODE_F12:
                    return Key.F12;
                case SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN:
                    return Key.PrintScreen;
                case SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK:
                    return Key.ScrollLock;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAUSE:
                    return Key.Pause;
                case SDL.SDL_Scancode.SDL_SCANCODE_INSERT:
                    return Key.Insert;
                case SDL.SDL_Scancode.SDL_SCANCODE_HOME:
                    return Key.Home;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP:
                    return Key.PageUp;
                case SDL.SDL_Scancode.SDL_SCANCODE_DELETE:
                    return Key.Delete;
                case SDL.SDL_Scancode.SDL_SCANCODE_END:
                    return Key.End;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
                    return Key.PageDown;
                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT:
                    return Key.Right;
                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT:
                    return Key.Left;
                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN:
                    return Key.Down;
                case SDL.SDL_Scancode.SDL_SCANCODE_UP:
                    return Key.Up;
                case SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR:
                    return Key.NumLock;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE:
                    return Key.KeypadDivide;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY:
                    return Key.KeypadMultiply;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS:
                    return Key.KeypadMinus;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS:
                    return Key.KeypadPlus;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER:
                    return Key.KeypadEnter;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_1:
                    return Key.Keypad1;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_2:
                    return Key.Keypad2;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_3:
                    return Key.Keypad3;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_4:
                    return Key.Keypad4;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_5:
                    return Key.Keypad5;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_6:
                    return Key.Keypad6;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_7:
                    return Key.Keypad7;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_8:
                    return Key.Keypad8;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_9:
                    return Key.Keypad9;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_0:
                    return Key.Keypad0;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD:
                    return Key.KeypadPeriod;
                case SDL.SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH:
                    return Key.NonUSBackSlash;
                case SDL.SDL_Scancode.SDL_SCANCODE_KP_EQUALS:
                    return Key.KeypadPlus;
                case SDL.SDL_Scancode.SDL_SCANCODE_F13:
                    return Key.F13;
                case SDL.SDL_Scancode.SDL_SCANCODE_F14:
                    return Key.F14;
                case SDL.SDL_Scancode.SDL_SCANCODE_F15:
                    return Key.F15;
                case SDL.SDL_Scancode.SDL_SCANCODE_F16:
                    return Key.F16;
                case SDL.SDL_Scancode.SDL_SCANCODE_F17:
                    return Key.F17;
                case SDL.SDL_Scancode.SDL_SCANCODE_F18:
                    return Key.F18;
                case SDL.SDL_Scancode.SDL_SCANCODE_F19:
                    return Key.F19;
                case SDL.SDL_Scancode.SDL_SCANCODE_F20:
                    return Key.F20;
                case SDL.SDL_Scancode.SDL_SCANCODE_F21:
                    return Key.F21;
                case SDL.SDL_Scancode.SDL_SCANCODE_F22:
                    return Key.F22;
                case SDL.SDL_Scancode.SDL_SCANCODE_F23:
                    return Key.F23;
                case SDL.SDL_Scancode.SDL_SCANCODE_F24:
                    return Key.F24;
                case SDL.SDL_Scancode.SDL_SCANCODE_MENU:
                    return Key.Menu;
                case SDL.SDL_Scancode.SDL_SCANCODE_LCTRL:
                    return Key.ControlLeft;
                case SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT:
                    return Key.ShiftLeft;
                case SDL.SDL_Scancode.SDL_SCANCODE_LALT:
                    return Key.AltLeft;
                case SDL.SDL_Scancode.SDL_SCANCODE_RCTRL:
                    return Key.ControlRight;
                case SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT:
                    return Key.ShiftRight;
                case SDL.SDL_Scancode.SDL_SCANCODE_RALT:
                    return Key.AltRight;
                default:
                    return Key.Unknown;
            }
        }

        private static MouseButton MapMouseButton(byte button)
        {
            switch (button)
            {
                case 1:
                    return MouseButton.Left;
                case 2:
                    return MouseButton.Middle;
                case 3:
                    return MouseButton.Right;
                case 4:
                    return MouseButton.Button1;
                case 5:
                    return MouseButton.Button2;
                default:
                    return MouseButton.Left;
            }
        }

        private static ModifierKeys MapModifierKeys(SDL.SDL_Keymod mod)
        {
            ModifierKeys mods = ModifierKeys.None;
            if ((mod & (SDL.SDL_Keymod.KMOD_LSHIFT | SDL.SDL_Keymod.KMOD_RSHIFT)) != 0)
            {
                mods |= ModifierKeys.Shift;
            }
            if ((mod & (SDL.SDL_Keymod.KMOD_LALT | SDL.SDL_Keymod.KMOD_RALT)) != 0)
            {
                mods |= ModifierKeys.Alt;
            }
            if ((mod & (SDL.SDL_Keymod.KMOD_LCTRL | SDL.SDL_Keymod.KMOD_RCTRL)) != 0)
            {
                mods |= ModifierKeys.Control;
            }

            return mods;
        }
    }
}
