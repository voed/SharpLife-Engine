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

using SharpLife.Game.Shared.Models;
using System;
using Veldrid;

namespace SharpLife.Game.Client.Renderer.Shared.Utility
{
    public sealed class RenderModePipelines
    {
        private readonly Pipeline[] _pipelines;

        public Pipeline this[RenderMode renderMode]
        {
            get => _pipelines[(int)renderMode];
        }

        public RenderModePipelines(Pipeline[] pipelines)
        {
            _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));

            if (pipelines.Length != (int)RenderMode.Last + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelines));
            }
        }
    }
}
