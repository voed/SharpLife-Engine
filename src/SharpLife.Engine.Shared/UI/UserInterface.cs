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

using SharpLife.Engine.Shared.Loop;
using SharpLife.FileSystem;
using System;

namespace SharpLife.Engine.Shared.UI
{
    public class UserInterface : IUserInterface
    {
        public IWindowManager WindowManager { get; }

        public IWindow MainWindow { get; private set; }

        public UserInterface(IFileSystem fileSystem, IEngineLoop engineLoop)
        {
            WindowManager = new WindowManager(fileSystem, engineLoop);
        }

        public IWindow CreateMainWindow(string title, SDL2.SDL.SDL_WindowFlags additionalFlags = 0, bool recreate = false)
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
    }
}
