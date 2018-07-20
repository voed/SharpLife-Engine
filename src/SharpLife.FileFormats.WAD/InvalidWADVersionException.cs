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

namespace SharpLife.FileFormats.WAD
{
#pragma warning disable RCS1194 // Implement exception constructors.
    public sealed class InvalidWADVersionException : Exception
#pragma warning restore RCS1194 // Implement exception constructors.
    {
        public uint Version { get; }

        public InvalidWADVersionException(uint version)
        {
            Version = version;
        }

        public InvalidWADVersionException(uint version, string message)
            : base(message)
        {
            Version = version;
        }

        public InvalidWADVersionException(uint version, string message, Exception innerException)
            : base(message, innerException)
        {
            Version = version;
        }
    }
}
