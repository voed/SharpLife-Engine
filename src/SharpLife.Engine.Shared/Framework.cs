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

using SharpLife.Utility;

namespace SharpLife.Engine.Shared
{
    /// <summary>
    /// Framework constants
    /// </summary>
    public static class Framework
    {
        /// <summary>
        /// This is the default language that game data is localized in
        /// </summary>
        public const string DefaultLanguage = "english";

        /// <summary>
        /// Name of the model that will be used as a fallback when a model fails to load
        /// </summary>
        public static readonly string FallbackModelName = System.IO.Path.Combine(Directory.Models, "error" + FileExtensionUtils.AsExtension(Extension.MDL));

        public const string BSPModelNamePrefix = "*";

        public static class Directory
        {
            public const string Graphics = "gfx";
            public const string EnvironmentMaps = "env";
            public const string Shaders = "shaders";
            public const string Maps = "maps";
            public const string Models = "models";
        }

        public static class Path
        {
            /// <summary>
            /// 2D skybox textures are stored here
            /// </summary>
            public static readonly string EnvironmentMaps = System.IO.Path.Combine(Directory.Graphics, Directory.EnvironmentMaps);

            /// <summary>
            /// Shared are stored here
            /// </summary>
            public static readonly string Shaders = System.IO.Path.Combine(Directory.Graphics, Directory.Shaders);

            /// <summary>
            /// Maps are stored here
            /// </summary>
            public static readonly string Maps = Directory.Maps;
        }

        /// <summary>
        /// Contains commonly used file extensions, without the dot
        /// Use <see cref="SharpLife.Utility.FileExtensionUtils.AsExtension(string)"/> to add the dot
        /// </summary>
        public static class Extension
        {
            public const string WAD = "wad";
            public const string BSP = "bsp";
            public const string MDL = "mdl";
            public const string CFG = "cfg";
        }
    }
}
