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

using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Game.Shared.Networking.Conversion;
using SharpLife.Models;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;
using System;
using System.Numerics;

namespace SharpLife.Game.Shared.Networking
{
    public static class SharedObjectListTypes
    {
        public static void RegisterSharedTypes(TypeRegistryBuilder typeRegistryBuilder, IEngineModels engineModels)
        {
            if (typeRegistryBuilder == null)
            {
                throw new ArgumentNullException(nameof(typeRegistryBuilder));
            }

            typeRegistryBuilder.RegisterArrayConverter(typeof(ListConverter<>));

            typeRegistryBuilder.RegisterType(typeof(bool), BooleanConverter.Instance);

            typeRegistryBuilder.RegisterType(typeof(sbyte), Int8Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(short), Int16Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(int), Int32Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(long), Int64Converter.Instance);

            typeRegistryBuilder.RegisterType(typeof(byte), UInt8Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(ushort), UInt16Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(uint), UInt32Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(ulong), UInt64Converter.Instance);

            typeRegistryBuilder.RegisterType(typeof(float), FloatConverter.Instance);
            typeRegistryBuilder.RegisterType(typeof(double), DoubleConverter.Instance);

            typeRegistryBuilder.RegisterType(typeof(string), StringConverter.Instance);

            typeRegistryBuilder.RegisterType(typeof(Vector2), Vector2Converter.Instance);
            typeRegistryBuilder.RegisterType(typeof(Vector3), Vector3Converter.Instance);

            typeRegistryBuilder.RegisterType(typeof(IModel), new ModelConverter(engineModels));
        }
    }
}
