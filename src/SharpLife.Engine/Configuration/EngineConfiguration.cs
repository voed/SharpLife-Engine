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

namespace SharpLife.Engine.Configuration
{
    public sealed class EngineConfiguration
    {
        /// <summary>
        /// The mod name of the default game to load
        /// </summary>
        public string DefaultGame { get; set; }

        /// <summary>
        /// The name of the default game to use for display purposes
        /// </summary>
        public string DefaultGameName { get; set; }

        /// <summary>
        /// Whether to enable HD models
        /// </summary>
        public bool EnableHDModels { get; set; } = true;

        /// <summary>
        /// Whether to enable the addons directory for file searching
        /// </summary>
        public bool EnableAddonsFolder { get; set; }
    }
}
