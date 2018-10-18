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
using SharpLife.Models.SPR.FileFormat;
using System;
using System.Numerics;

namespace SharpLife.Game.Shared.Models.SPR
{
    public sealed class SpriteModel : BaseModel
    {
        public SpriteFile SpriteFile { get; }

        public SpriteModel(string name, uint crc, SpriteFile spriteFile)
            : base(name,
                  crc,
                  new Vector3(spriteFile.MaximumWidth / -2, spriteFile.MaximumWidth / -2, spriteFile.MaximumHeight / -2),
                  new Vector3(spriteFile.MaximumWidth / 2, spriteFile.MaximumWidth / 2, spriteFile.MaximumHeight / 2))
        {
            SpriteFile = spriteFile ?? throw new ArgumentNullException(nameof(spriteFile));
        }
    }
}
