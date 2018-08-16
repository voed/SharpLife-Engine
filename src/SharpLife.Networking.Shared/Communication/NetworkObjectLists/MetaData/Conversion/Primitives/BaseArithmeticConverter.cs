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

using Generic.Math;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives
{
    /// <summary>
    /// Provides behavior shared between arithmetic type converters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseArithmeticConverter<T> : BaseValueTypeConverter<T>
        where T : unmanaged
    {
        public override int MemberCount => 1;

        public override bool Encode(in T value, in T previousValue, out T result)
        {
            result = GenericMath.Subtract(value, previousValue);

            return GenericMath.NotEqual(result, default);
        }

        public override void Decode(in T value, in T previousValue, out T result)
        {
            result = GenericMath.Add(value, previousValue);
        }
    }
}
