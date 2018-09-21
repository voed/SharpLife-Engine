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

using SharpLife.Utility.Mathematics;
using System.Numerics;

namespace SharpLife.Game.Client.Renderer.Shared
{
    /// <summary>
    /// Provides read-only access to the renderer view state
    /// </summary>
    public interface IViewState
    {
        Vector3 Origin { get; }

        Vector3 Angles { get; }

        /// <summary>
        /// Gets the view angles as directional vectors
        /// </summary>
        DirectionalVectors ViewVectors { get; }
    }
}
