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

using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion;
using System;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    /// <summary>
    /// Used to specify bit converter options on networked members
    /// If not provided, <see cref="BitConverterOptions.Default"/> is used
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BitConverterOptionsAttribute : Attribute
    {
        public uint Bits { get; }

        public float Multiplier { get; }

        public float PostMultiplier { get; }

        public BitConverterFlags Flags { get; }

        /// <summary>
        /// Returns this attribute's options as a <see cref="BitConverterOptions"/> instance
        /// </summary>
        public BitConverterOptions Options => new BitConverterOptions(Bits, Multiplier, PostMultiplier, Flags);

        public BitConverterOptionsAttribute(
            uint bits,
            float multiplier = BitConverterOptions.NoMultiplication,
            float postMultiplier = BitConverterOptions.NoMultiplication,
            BitConverterFlags flags = BitConverterFlags.None)
        {
            Bits = bits;
            Multiplier = multiplier;
            PostMultiplier = postMultiplier;
            Flags = flags;
        }
    }
}
