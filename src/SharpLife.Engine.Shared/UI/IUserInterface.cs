using SDL2;
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

namespace SharpLife.Engine.Shared.UI
{
    /// <summary>
    /// Controls the User Interface components
    /// </summary>
    public interface IUserInterface
    {
        /// <summary>
        /// All windows used by the user interface are managed by this
        /// </summary>
        IWindowManager WindowManager { get; }

        /// <summary>
        /// The main game window is typically where the game itself is rendered to and where user input is taken from
        /// </summary>
        IWindow MainWindow { get; }

        /// <summary>
        /// Creates the main window
        /// </summary>
        /// <param name="title"></param>
        /// <param name="additionalFlags"></param>
        /// <param name="recreate">If false, and the main window already exists, an exception will be thrown</param>
        /// <exception cref="System.InvalidOperationException">If the main window already exists, and recreate is false</exception>
        IWindow CreateMainWindow(string title, SDL.SDL_WindowFlags additionalFlags = 0, bool recreate = false);

        /// <summary>
        /// Destroys the main window if exists
        /// </summary>
        void DestroyMainWindow();

        /// <summary>
        /// Sleep up to <paramref name="milliSeconds"/> milliseconds, waking to process events
        /// </summary>
        /// <param name="milliSeconds"></param>
        void SleepUntilInput(int milliSeconds);

        /// <summary>
        /// Shuts down the user interface
        /// The interface can no longer be used after this
        /// </summary>
        void Shutdown();
    }
}
