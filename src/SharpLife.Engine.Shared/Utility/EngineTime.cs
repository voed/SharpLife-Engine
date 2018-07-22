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
using System.Diagnostics;

namespace SharpLife.Engine.Shared.Utility
{
    public class EngineTime : IEngineTime
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public long Frequency => Stopwatch.Frequency;

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public long ElapsedTicks => _stopwatch.ElapsedTicks;

        public bool IsRunning => _stopwatch.IsRunning;

        public EngineTime()
        {
        }

        /// <summary>
        /// Creates an engine time that adopts the given stopwatch
        /// </summary>
        /// <param name="stopwatch"></param>
        public EngineTime(Stopwatch stopwatch)
        {
            _stopwatch = stopwatch ?? throw new ArgumentNullException(nameof(stopwatch));
        }

        public void Start() => _stopwatch.Start();

        public void Stop() => _stopwatch.Stop();

        public void Reset() => _stopwatch.Reset();

        public void Restart() => _stopwatch.Restart();

        public override string ToString() => _stopwatch.ToString();
    }
}
