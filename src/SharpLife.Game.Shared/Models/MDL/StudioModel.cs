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

using SharpLife.Models;
using SharpLife.Models.MDL.FileFormat;
using System;
using System.Numerics;

namespace SharpLife.Game.Shared.Models.MDL
{
    public sealed class StudioModel : BaseModel
    {
        public StudioFile StudioFile { get; }

        public StudioModel(string name, uint crc, StudioFile studioFile)
            //TODO: figure out if models have a preset size
            : base(name, crc, Vector3.Zero, Vector3.Zero)
        {
            StudioFile = studioFile ?? throw new ArgumentNullException(nameof(studioFile));
        }
    }
}
