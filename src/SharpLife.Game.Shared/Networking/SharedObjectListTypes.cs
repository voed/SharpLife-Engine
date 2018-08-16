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

using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;
using System;
using System.Numerics;

namespace SharpLife.Game.Shared.Networking
{
    public static class SharedObjectListTypes
    {
        public static void RegisterSharedTypes(TypeRegistry typeRegistry)
        {
            if (typeRegistry == null)
            {
                throw new ArgumentNullException(nameof(typeRegistry));
            }

            typeRegistry.RegisterArrayConverter(typeof(ListConverter<>));

            typeRegistry.RegisterType(typeof(bool), BooleanConverter.Instance);

            typeRegistry.RegisterType(typeof(sbyte), Int8Converter.Instance);
            typeRegistry.RegisterType(typeof(short), Int16Converter.Instance);
            typeRegistry.RegisterType(typeof(int), Int32Converter.Instance);
            typeRegistry.RegisterType(typeof(long), Int64Converter.Instance);

            typeRegistry.RegisterType(typeof(byte), UInt8Converter.Instance);
            typeRegistry.RegisterType(typeof(ushort), UInt16Converter.Instance);
            typeRegistry.RegisterType(typeof(uint), UInt32Converter.Instance);
            typeRegistry.RegisterType(typeof(ulong), UInt64Converter.Instance);

            typeRegistry.RegisterType(typeof(float), FloatConverter.Instance);
            typeRegistry.RegisterType(typeof(double), DoubleConverter.Instance);

            typeRegistry.RegisterType(typeof(string), StringConverter.Instance);

            typeRegistry.RegisterType(typeof(Vector2), Vector2Converter.Instance);
            typeRegistry.RegisterType(typeof(Vector3), Vector3Converter.Instance);
        }
    }
}
