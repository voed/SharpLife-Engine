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
    public class LoggingConfiguration
    {
        public enum Format
        {
            Text,
            CompactJSON
        }

        /// <summary>
        /// The maximum number of days to retain old log files
        /// If not specified, logs are kept forever
        /// </summary>
        public int? RetainedFileCountLimit { get; set; }

        /// <summary>
        /// Which format to output log data as
        /// </summary>
        public Format LogFormat { get; set; }
    }
}
