﻿/***
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

namespace SharpLife.Utility
{
    /// <summary>
    /// Provides a means to query time
    /// </summary>
    public interface ITime
    {
        TimeSpan Elapsed { get; }

        long ElapsedMilliseconds { get; }

        /// <summary>
        /// Time in seconds
        /// </summary>
        double ElapsedTime { get; }

        /// <summary>
        /// Time between the previous and current frame
        /// </summary>
        double FrameTime { get; }
    }
}
