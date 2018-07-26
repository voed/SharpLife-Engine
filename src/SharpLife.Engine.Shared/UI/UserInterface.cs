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
using System;

namespace SharpLife.Engine.Shared.UI
{
    public class UserInterface : IUserInterface
    {
        public IWindowManager WindowManager { get; }

        public IWindow MainWindow { get; private set; }

        public UserInterface(ILogger logger, IFileSystem fileSystem, IEngineLoop engineLoop, bool noOnTop)
        {
            //Disable to prevent debugger from shutting down the game
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            if (noOnTop)
            {
                SDL.SDL_SetHint(SDL.SDL_HINT_ALLOW_TOPMOST, "0");
            }

            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_XRANDR, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_XVIDMODE, "1");

            SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);

            WindowManager = new WindowManager(logger, fileSystem, engineLoop);
        }

        public IWindow CreateMainWindow(string title, SDL.SDL_WindowFlags additionalFlags = 0, bool recreate = false)
        {
            if (MainWindow != null)
            {
                if (!recreate)
                {
                    throw new InvalidOperationException("Cannot recreate main window when recreate is not requested");
                }

                DestroyMainWindow();
            }

            MainWindow = WindowManager.CreateWindow(title, additionalFlags);

            return MainWindow;
        }

        public void DestroyMainWindow()
        {
            if (MainWindow != null)
            {
                WindowManager.DestroyWindow(MainWindow);
                MainWindow = null;
            }
        }

        /// <summary>
        /// Sleep up to <paramref name="milliSeconds"/> milliseconds, waking to process events
        /// </summary>
        /// <param name="milliSeconds"></param>
        public void SleepUntilInput(int milliSeconds)
        {
            WindowManager.SleepUntilInput(milliSeconds);
        }

        public void Shutdown()
        {
            SDL.SDL_Quit();
        }

        public void ShowMessageBox(MessageBoxIcon icon, string title, string message)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            SDL.SDL_MessageBoxFlags iconFlag = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;

            switch (icon)
            {
                default:
                    iconFlag = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;
                    break;
                case MessageBoxIcon.Warning:
                    iconFlag = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING;
                    break;
                case MessageBoxIcon.Error:
                    iconFlag = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR;
                    break;
            }

            SDL.SDL_ShowSimpleMessageBox(iconFlag, title, message, IntPtr.Zero);
        }
    }
}
