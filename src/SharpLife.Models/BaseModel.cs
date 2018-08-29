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

namespace SharpLife.Models
{
    public abstract class BaseModel : IModel
    {
        public string Name { get; }

        public uint CRC { get; }

        protected BaseModel(string name, uint crc)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            CRC = crc;
        }
    }
}
