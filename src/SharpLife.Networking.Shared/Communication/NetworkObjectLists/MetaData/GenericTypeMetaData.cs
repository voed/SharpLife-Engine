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
using System.Linq;
using System.Reflection;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    /// <summary>
    /// Stores information about a generic type and provides a way to create instances of converters for specific instances
    /// </summary>
    public sealed class GenericTypeMetaData
    {
        public Type GenericType { get; }

        public TypeInfo ConverterType { get; }

        internal GenericTypeMetaData(Type genericType, TypeInfo converterType)
        {
            GenericType = genericType ?? throw new ArgumentNullException(nameof(genericType));
            ConverterType = converterType ?? throw new ArgumentNullException(nameof(converterType));
        }

        /// <summary>
        /// Creates a converter that can convert a generic type instance that has the given types
        /// </summary>
        /// <param name="registryBuilder"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        internal ITypeConverter CreateConverter(TypeRegistryBuilder registryBuilder, params Type[] types)
        {
            if (registryBuilder == null)
            {
                throw new ArgumentNullException(nameof(registryBuilder));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (ConverterType.GenericTypeParameters.Length != types.Length)
            {
                throw new ArgumentException(
                    $"Generic type {ConverterType.FullName} instantiation requested {types.Length} type arguments, {ConverterType.GenericTypeParameters.Length} required",
                    nameof(types));
            }

            var uniqueTypes = types.Distinct().ToArray();

            //Look up the type meta data for each type so we can get the converter for each type
            var typesMetaData = new TypeMetaData[uniqueTypes.Length];

            for (var i = 0; i < uniqueTypes.Length; ++i)
            {
                var type = uniqueTypes[i];

                var metaData = registryBuilder.FindMetaDataByType(type);

                if (metaData == null)
                {
                    throw new InvalidOperationException($"Generic type {ConverterType.FullName} instantion is referencing type {type.FullName}, which has not been registered");
                }

                if (metaData.Converter == null)
                {
                    throw new InvalidOperationException($"Generic type {ConverterType.FullName} instantion is referencing type {type.FullName}, which has no converter associated with it");
                }

                typesMetaData[i] = metaData;
            }

            var instanceType = ConverterType.MakeGenericType(types);

            //Find a constructor that takes the unique arguments in the order that they first occurred
            var converterTypes = typesMetaData.Select(subConverter => subConverter.Converter.GetType()).ToArray();

            var constructor = instanceType.GetConstructor(converterTypes);

            if (constructor == null)
            {
                throw new InvalidOperationException($"Generic type converter {ConverterType.FullName} does not have a constructor taking each generic type converter");
            }

            var subConverters = typesMetaData.Select(subConverter => subConverter.Converter).ToArray();

            return (ITypeConverter)constructor.Invoke(subConverters);
        }
    }
}
