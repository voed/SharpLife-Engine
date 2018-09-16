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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.BitConverters
{
    /// <summary>
    /// Options for bit converter types
    /// </summary>
    public readonly struct BitConverterOptions
    {
        /// <summary>
        /// Apply no multiplication to values
        /// </summary>
        public const float NoMultiplication = 1.0f;

        public const float MultiplierEpsilon = 0.001f;

        /// <summary>
        /// Number of bits to send, including sign bit if specified in <seealso cref="Flags"/>
        /// </summary>
        public readonly uint Bits;

        /// <summary>
        /// Multiplier applied before sending and after reception
        /// </summary>
        public readonly float Multiplier;

        /// <summary>
        /// Applied after being received
        /// </summary>
        public readonly float PostMultiplier;

        public readonly BitConverterFlags Flags;

        public static bool ShouldMultiplyWithMultiplier(float value)
        {
            return value < (1.0f - MultiplierEpsilon) || (1.0f + MultiplierEpsilon) < value;
        }

        public bool ShouldMultiply => ShouldMultiplyWithMultiplier(Multiplier);

        public bool ShouldPostMultiply => ShouldMultiplyWithMultiplier(PostMultiplier);

        public BitConverterOptions(
            uint bits,
            float multiplier = NoMultiplication,
            float postMultiplier = NoMultiplication,
            BitConverterFlags flags = BitConverterFlags.None)
        {
            Bits = bits;
            Multiplier = multiplier;
            PostMultiplier = postMultiplier;
            Flags = flags;
        }
    }
}
