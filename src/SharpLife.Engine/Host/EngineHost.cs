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
using SharpLife.Engine.Engines;
using System;

namespace SharpLife.Engine.Host
{
    /// <summary>
    /// Handles engine hosting, startup
    /// </summary>
    public sealed class EngineHost
    {
        public void Start(string[] args, HostType type)
        {
            try
            {
                IEngine engine = new ClientServerEngine();

                engine.Run(args);
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
            catch (Exception e)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
            {
                //Log first, in case user terminates program while messagebox is open
                Log.Logger?.Error(e, "A fatal error occurred");

                //Display an error message
                SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "SharpLife error", e.Message, IntPtr.Zero);

                throw;
            }
        }
    }
}
