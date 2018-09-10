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

using System;

namespace SharpLife.Engine.Shared.UI
{
    /// <summary>
    /// Provides access to an SDL2 window and its associated input system
    /// </summary>
    public interface IWindow
    {
        bool Exists { get; }

        string Title { get; set; }

        //TODO: maybe make this internal so only code that needs to access these handles can get them?
        IntPtr WindowHandle { get; }

        event Action Resized;

        /// <summary>
        /// Invoked when the window is about to be destroyed
        /// </summary>
        event Action Destroying;

        /// <summary>
        /// Invoked after the window has been destroyed
        /// </summary>
        event Action Destroyed;

        void Center();
    }
}
