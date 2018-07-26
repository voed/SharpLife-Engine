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
using Serilog;
using SharpLife.Engine.Shared.Loop;
using SharpLife.FileSystem;
using SharpLife.Input;
using System;
using System.Collections.Generic;

namespace SharpLife.Engine.Shared.UI
{
    public sealed class WindowManager : IWindowManager
    {
        public IInputSystem InputSystem { get; } = new InputSystem();

        private readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;

        private readonly IEngineLoop _engineLoop;

        private readonly Dictionary<IntPtr, Window> _windows = new Dictionary<IntPtr, Window>();

        public WindowManager(ILogger logger, IFileSystem fileSystem, IEngineLoop engineLoop)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _engineLoop = engineLoop ?? throw new ArgumentNullException(nameof(engineLoop));
        }

        public IWindow CreateWindow(string title, SDL.SDL_WindowFlags additionalFlags = 0)
        {
            var window = new Window(_logger, _fileSystem, title, additionalFlags);

            _windows.Add(window.WindowHandle, window);

            return window;
        }

        public void DestroyWindow(IWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (!_windows.TryGetValue(window.WindowHandle, out var internalWindow))
            {
                throw new ArgumentException($"Cannot destroy window \"{window.Title}\" that is not owned by this manager");
            }

            _windows.Remove(internalWindow.WindowHandle);

            internalWindow.Destroy();
        }

        public void DestroyAllWindows()
        {
            foreach (var window in _windows.Values)
            {
                window.Destroy();
            }

            _windows.Clear();
        }

        public void SleepUntilInput(int milliSeconds)
        {
            InputSystem.ProcessEvents(milliSeconds);

            var snapshot = InputSystem.Snapshot;

            for (var i = 0; i < snapshot.Events.Count; ++i)
            {
                var sdlEvent = snapshot.Events[i];

                switch (sdlEvent.type)
                {
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        {
                            var windowID = sdlEvent.window.windowID;

                            var windowHandle = SDL.SDL_GetWindowFromID(windowID);

                            if (windowHandle != IntPtr.Zero)
                            {
                                if (_windows.TryGetValue(windowHandle, out var window))
                                {
                                    window.ProcessEvent(ref sdlEvent);
                                }
                            }

                            break;
                        }
                    case SDL.SDL_EventType.SDL_QUIT:
                        {
                            _engineLoop.Exiting = true;
                            break;
                        }
                }
            }
        }
    }
}
