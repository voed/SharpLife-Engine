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

namespace SharpLife.Utility
{
    public static class FileSystemConstants
    {
        public static class PathID
        {
            /// <summary>
            /// All path IDs will be searched
            /// </summary>
            public const string All = null;
            public const string DefaultGame = "DEFAULTGAME";
            public const string Game = "GAME";
            public const string Platform = "PLATFORM";
            public const string GameConfig = "GAMECONFIG";
            public const string GameDownload = "GAMEDOWNLOAD";
            public const string Base = "BASE";
        }

        public static class Suffixes
        {
            public const string LowViolence = "_lv";
            public const string Addon = "_addon";
            public const string HD = "_hd";
            public const string Downloads = "_downloads";
        }

        public const string PlatformDirectory = "platform";
    }
}
