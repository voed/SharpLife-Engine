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

namespace SharpLife.Utility.Events
{
    /// <summary>
    /// Base class for delegate invokers
    /// </summary>
    internal abstract class Invoker
    {
        /// <summary>
        /// The object that the delegate wraps around
        /// Used to identify delegates owned by an object
        /// </summary>
        public abstract object Target { get; }

        /// <summary>
        /// The delegate that the user passed in
        /// </summary>
        public abstract Delegate Delegate { get; }

        public abstract void Invoke(in Event @event);
    }
}
