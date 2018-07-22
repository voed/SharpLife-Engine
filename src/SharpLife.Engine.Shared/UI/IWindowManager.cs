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
using SharpLife.Input;

namespace SharpLife.Engine.Shared.UI
{
    public interface IWindowManager
    {
        IInputSystem InputSystem { get; }

        IWindow CreateWindow(string title, SDL.SDL_WindowFlags additionalFlags = 0);

        void DestroyWindow(IWindow window);

        /// <summary>
        /// Destroys all windows
        /// All windows will become unusable
        /// </summary>
        void DestroyAllWindows();

        /// <summary>
        /// Sleep up to <paramref name="milliSeconds"/> milliseconds, waking to process events
        /// </summary>
        /// <param name="milliSeconds"></param>
        void SleepUntilInput(int milliSeconds);
    }
}
