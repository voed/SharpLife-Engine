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
using SharpLife.Engine.Configuration;
using SharpLife.Engine.Loop;
using SharpLife.Engine.Utility;
using SixLabors.ImageSharp;
using System;
using System.Runtime.InteropServices;

namespace SharpLife.Engine.Video
{
    /// <summary>
    /// Manages the SDL2 window
    /// </summary>
    public sealed class Window
    {
        private readonly ICommandLine _commandLine;

        private readonly GameConfiguration _gameConfiguration;

        private readonly IEngineLoop _engineLoop;

        private IntPtr _window;

        private IntPtr _glContext;

        public Window(ICommandLine commandLine, GameConfiguration gameConfiguration, IEngineLoop engineLoop)
        {
            _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
            _gameConfiguration = gameConfiguration ?? throw new ArgumentNullException(nameof(gameConfiguration));
            _engineLoop = engineLoop ?? throw new ArgumentNullException(nameof(engineLoop));

            if (commandLine.Contains("-noontop"))
            {
                SDL.SDL_SetHint(SDL.SDL_HINT_ALLOW_TOPMOST, "0");
            }

            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_XRANDR, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_XVIDMODE, "1");

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
        }

        ~Window()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void DestroyWindow()
        {
            if (_glContext != IntPtr.Zero)
            {
                SDL.SDL_GL_DeleteContext(_glContext);
                _glContext = IntPtr.Zero;
            }

            if (_window != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(_window);
                _window = IntPtr.Zero;
            }
        }

        private void Destroy()
        {
            DestroyWindow();

            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_VIDEO);
        }

        public void CreateGameWindow()
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCELERATED_VISUAL, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);

            var gameWindowName = "Half-Life";

            if (!string.IsNullOrWhiteSpace(_gameConfiguration.GameName))
            {
                gameWindowName = _gameConfiguration.GameName;
            }

            var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;

            if (_commandLine.Contains("-noborder"))
            {
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            }

            _window = SDL.SDL_CreateWindow(gameWindowName, 0, 0, 640, 480, flags);

            if (_window == IntPtr.Zero)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 3);
                _window = SDL.SDL_CreateWindow(gameWindowName, 0, 0, 640, 480, flags);

                if (_window == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to create SDL Window");
                }
            }

            //Load the game icon
            var image = Image.Load("sharplife_full/game.png");

            if (image != null)
            {
                var pixels = image.SavePixelData();

                var nativeMemory = Marshal.AllocHGlobal(pixels.Length);

                Marshal.Copy(pixels, 0, nativeMemory, pixels.Length);

                var surface = SDL.SDL_CreateRGBSurfaceFrom(nativeMemory, image.Width, image.Height, 32, 4 * image.Width, 0xFF, 0xFF << 8, 0xFF << 16, unchecked((uint)(0xFF << 24)));

                if (surface != IntPtr.Zero)
                {
                    SDL.SDL_SetWindowIcon(_window, surface);
                    SDL.SDL_FreeSurface(surface);
                }

                Marshal.FreeHGlobal(nativeMemory);
            }

            SDL.SDL_ShowWindow(_window);

            _glContext = SDL.SDL_GL_CreateContext(_window);

            if (_glContext == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create SDL Window");
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, out var r))
            {
                r = 0;
                Console.WriteLine("Failed to get GL RED size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, out var g))
            {
                g = 0;
                Console.WriteLine("Failed to get GL GREEN size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, out var b))
            {
                b = 0;
                Console.WriteLine("Failed to get GL BLUE size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, out var a))
            {
                a = 0;
                Console.WriteLine("Failed to get GL ALPHA size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, out var depth))
            {
                depth = 0;
                Console.WriteLine("Failed to get GL DEPTH size ({0})", SDL.SDL_GetError());
            }

            Console.WriteLine($"GL_SIZES:  r:{r} g:{g} b:{b} a:{a} depth:{depth}");

            if (r <= 4 || g <= 4 || b <= 4 || depth <= 15 /*|| gl_renderer && Q_strstr(gl_renderer, "GDI Generic")*/)
            {
                throw new InvalidOperationException("Failed to create SDL Window, unsupported video mode. A 16-bit color depth desktop is required and a supported GL driver");
            }
        }

        public void CenterWindow()
        {
            SDL.SDL_GetWindowSize(_window, out var windowWidth, out var windowHeight);

            if (0 == SDL.SDL_GetDisplayBounds(0, out var bounds))
            {
                SDL.SDL_SetWindowPosition(_window, (bounds.w - windowWidth) / 2, (bounds.h - windowHeight) / 2);
            }
        }

        /// <summary>
        /// Sleep up to <paramref name="milliSeconds"/> milliseconds, waking to process events
        /// </summary>
        /// <param name="milliSeconds"></param>
        public void SleepUntilInput(int milliSeconds)
        {
            SDL.SDL_PumpEvents();

            if (SDL.SDL_WaitEventTimeout(out var sdlEvent, milliSeconds) > 0)
            {
                //Process all events
                do
                {
                    switch (sdlEvent.type)
                    {
                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            {
                                if (sdlEvent.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
                                {
                                    _engineLoop.Exiting = true;
                                    DestroyWindow();
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
                while (SDL.SDL_PollEvent(out sdlEvent) > 0);
            }
        }
    }
}
