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

namespace SharpLife.Input
{
    /// <summary>
    /// Tracks the state of all user input
    /// </summary>
    public interface IInputSystem
    {
        /// <summary>
        /// Gets the current frame's snapshot
        /// If you need to store this snapshot, clone it using the <see cref="IInputSnapshot.Clone"/> method
        /// </summary>
        /// <returns></returns>
        IInputSnapshot Snapshot { get; }

        /// <summary>
        /// Sleep up to <paramref name="milliSeconds"/> milliseconds, waking to process events
        /// </summary>
        /// <param name="milliSeconds"></param>
        void ProcessEvents(int milliSeconds);
    }
}
