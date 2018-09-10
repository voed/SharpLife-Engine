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
using SharpLife.FileSystem;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpLife.Engine.Shared.UI
{
    /// <summary>
    /// Manages the SDL2 window
    /// </summary>
    internal sealed class Window : IWindow
    {
        public bool Exists => WindowHandle != IntPtr.Zero;

        public string Title
        {
            get => SDL.SDL_GetWindowTitle(WindowHandle);
            set => SDL.SDL_SetWindowTitle(WindowHandle, value);
        }

        public IntPtr WindowHandle { get; private set; }

        public event Action Resized;

        public event Action Destroying;

        public event Action Destroyed;

        public Window(ILogger logger, IFileSystem fileSystem, string title, SDL.SDL_WindowFlags additionalFlags = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            //This differs from vanilla GoldSource; set the OpenGL context version to 3.0 so we can use shaders
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL.SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCELERATED_VISUAL, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);

            var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL
                | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN
                | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
                | additionalFlags;

            WindowHandle = SDL.SDL_CreateWindow(title, 0, 0, 640, 480, flags);

            if (WindowHandle == IntPtr.Zero)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 3);
                WindowHandle = SDL.SDL_CreateWindow(title, 0, 0, 640, 480, flags);

                if (WindowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to create SDL Window");
                }
            }

            //Load the game icon
            try
            {
                var image = Image.Load(fileSystem.OpenRead("game.png"));

                if (image != null)
                {
                    var pixels = image.GetPixelSpan().ToArray();

                    var pixelsData = GCHandle.Alloc(pixels, GCHandleType.Pinned);

                    var surface = SDL.SDL_CreateRGBSurfaceFrom(pixelsData.AddrOfPinnedObject(), image.Width, image.Height, 32, 4 * image.Width, 0xFF, 0xFF << 8, 0xFF << 16, unchecked((uint)(0xFF << 24)));

                    if (surface != IntPtr.Zero)
                    {
                        SDL.SDL_SetWindowIcon(WindowHandle, surface);
                        SDL.SDL_FreeSurface(surface);
                    }

                    pixelsData.Free();
                }
            }
            catch (FileNotFoundException)
            {
                //If image doesn't exist, just ignore it
            }

            SDL.SDL_ShowWindow(WindowHandle);
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

        internal void Destroy()
        {
            if (Exists)
            {
                //Both a pre and post callback are provided to allow resource cleanup to occur at the right time
                Destroying?.Invoke();

                if (WindowHandle != IntPtr.Zero)
                {
                    SDL.SDL_DestroyWindow(WindowHandle);
                    WindowHandle = IntPtr.Zero;
                }

                Destroyed?.Invoke();
            }
        }

        public void Center()
        {
            SDL.SDL_GetWindowSize(WindowHandle, out var windowWidth, out var windowHeight);

            if (0 == SDL.SDL_GetDisplayBounds(0, out var bounds))
            {
                SDL.SDL_SetWindowPosition(WindowHandle, (bounds.w - windowWidth) / 2, (bounds.h - windowHeight) / 2);
            }
        }

        internal void ProcessEvent(ref SDL.SDL_Event sdlEvent)
        {
            switch (sdlEvent.type)
            {
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    {
                        switch (sdlEvent.window.windowEvent)
                        {
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                                {
                                    Resized?.Invoke();
                                    break;
                                }

                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                {
                                    Destroy();
                                    break;
                                }
                        }
                        break;
                    }
            }
        }
    }
}
