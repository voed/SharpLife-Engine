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

using SharpLife.Models.MDL.FileFormat;
using System;

namespace SharpLife.Models.MDL.Loading
{
    public sealed class StudioModel : BaseModel
    {
        public StudioFile StudioFile { get; }

        public StudioModel(string name, uint crc, StudioFile studioFile)
            : base(name, crc)
        {
            StudioFile = studioFile ?? throw new ArgumentNullException(nameof(studioFile));
        }
    }
}
