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

using SharpLife.Engine.Engines;
using SharpLife.Engine.Shared.Engines;
using SharpLife.Engine.Shared.UI;
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
            ClientServerEngine engine = null;

            try
            {
                engine = new ClientServerEngine();

                engine.Run(args, type);
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
            catch (Exception e)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
            {
                if (engine != null)
                {
                    //Log first, in case user terminates program while messagebox is open
                    engine.Logger?.Error(e, "A fatal error occurred");

                    //Display an error message if a user interface is available
                    engine.UserInterface?.ShowMessageBox(MessageBoxIcon.Error, "SharpLife error", e.Message);
                }

                throw;
            }
        }
    }
}
