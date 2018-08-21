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
using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    /// <summary>
    /// Registry of networkable types
    /// </summary>
    public sealed class TypeRegistry
    {
        private readonly Dictionary<Type, GenericTypeMetaData> _genericTypes = new Dictionary<Type, GenericTypeMetaData>();

        private readonly Dictionary<Type, TypeMetaData> _types = new Dictionary<Type, TypeMetaData>();

        private readonly Dictionary<Type, TypeMetaData> _remappedTypes = new Dictionary<Type, TypeMetaData>();

        private uint _nextId;

        private GenericTypeMetaData _arrayConverter;

        private readonly Dictionary<uint, TypeMetaData> _transmitterToReceiverMap = new Dictionary<uint, TypeMetaData>();

        internal GenericTypeMetaData FindGenericMetaDataByType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _genericTypes.TryGetValue(type, out var metaData);

            return metaData;
        }

        public TypeMetaData FindMetaDataByType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.TryGetValue(type, out var metaData);

            return metaData;
        }

        /// <summary>
        /// Looks up a root type's metadata
        /// Also considers remapped types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TypeMetaData LookupRootType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_types.TryGetValue(type, out var metaData))
            {
                return metaData;
            }

            _remappedTypes.TryGetValue(type, out metaData);

            return metaData;
        }

        public TypeMetaData FindMetaDataByTransmitterId(uint id)
        {
            if (!_transmitterToReceiverMap.TryGetValue(id, out var metaData))
            {
                throw new InvalidOperationException($"Transmitter metadata id {id} has no associated metadata");
            }

            return metaData;
        }

        /// <summary>
        /// Registers a generic type and a converter type associated with it
        /// </summary>
        /// <param name="genericType"></param>
        /// <param name="converterType"></param>
        public void RegisterGenericType(Type genericType, Type converterType)
        {
            if (genericType == null)
            {
                throw new ArgumentNullException(nameof(genericType));
            }

            if (converterType == null)
            {
                throw new ArgumentNullException(nameof(converterType));
            }

            if (!genericType.IsGenericType)
            {
                throw new ArgumentException($"Type {genericType.FullName} is not a generic type");
            }

            if (genericType.IsConstructedGenericType)
            {
                //Constructed generic types are concrete types, should either be registered automatically through generic type instantiations or manually
                throw new ArgumentException($"Generic type {genericType.FullName} is a constructed generic type");
            }

            if (!converterType.IsGenericType)
            {
                //Converters are required to be generic in order to handle conversion of the generic types
                throw new ArgumentException($"Converter type {converterType.FullName} is not a generic type");
            }

            if (converterType.IsConstructedGenericType)
            {
                throw new ArgumentException($"Converter type {converterType.FullName} is a constructed generic type");
            }

            if (_genericTypes.ContainsKey(genericType))
            {
                throw new ArgumentException($"Generic type {genericType.FullName} has already been registered");
            }

            var genericTypeInfo = genericType.GetTypeInfo();
            var converterTypeInfo = converterType.GetTypeInfo();

            //There is no way to compare the generic types so we can only rely on converter construction to test for compatibility
            if (genericTypeInfo.GenericTypeParameters.Length != converterTypeInfo.GenericTypeParameters.Length)
            {
                throw new ArgumentException($"Converter {converterType.FullName} is not compatible with generic type {genericType.FullName}");
            }

            _genericTypes.Add(genericType, new GenericTypeMetaData(genericType, converterTypeInfo));
        }

        public void RegisterArrayConverter(Type converterType)
        {
            if (converterType == null)
            {
                throw new ArgumentNullException(nameof(converterType));
            }

            if (_arrayConverter != null)
            {
                throw new ArgumentException("An array converter has already been registered", nameof(converterType));
            }

            if (!converterType.IsGenericType)
            {
                //Converters are required to be generic in order to handle conversion of the generic types
                throw new ArgumentException($"Converter type {converterType.FullName} is not a generic type");
            }

            if (converterType.IsConstructedGenericType)
            {
                throw new ArgumentException($"Converter type {converterType.FullName} is a constructed generic type");
            }

            if (converterType.GetTypeInfo().GenericTypeParameters.Length != 1)
            {
                throw new ArgumentException($"Converter type {converterType.FullName} must have exactly one generic parameter");
            }

            //The generic type for this isn't really useful, but it makes checking the meta data easier (albeit wrong)
            _arrayConverter = new GenericTypeMetaData(typeof(Array), converterType.GetTypeInfo());
        }

        /// <summary>
        /// Looks up a member type to see if it's registered
        /// If the type is a generic type instantiation, it will be registered if possible
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal TypeMetaData LookupMemberType(Type type)
        {
            if (_types.TryGetValue(type, out var metaData))
            {
                return metaData;
            }

            if (type.IsArray)
            {
                //Only one dimensional arrays are supported for networking
                if (!type.IsSZArray)
                {
                    throw new InvalidOperationException("Multidimensional arrays are not supported");
                }

                if (_arrayConverter == null)
                {
                    throw new InvalidOperationException("No converter has been provided for arrays");
                }

                var underlyingType = type.GetElementType();

                var arrayConverter = _arrayConverter.CreateConverter(this, underlyingType);

                return InternalRegisterType(type, TypeMetaData.DefaultFactory, arrayConverter, Array.Empty<TypeMetaData.Member>(), null);
            }

            if (type.IsEnum)
            {
                //Use the underlying type for networking
                return LookupMemberType(type.GetEnumUnderlyingType());
            }

            if (!type.IsGenericType)
            {
                //Let caller handle failure (more information available)
                return null;
            }

            if (!type.IsConstructedGenericType)
            {
                //This should never happen since only concrete types can be registered to begin with
                throw new ArgumentException($"Type {type.FullName} is a non-constructed generic type and cannot be used as networked member");
            }

            var genericType = type.GetGenericTypeDefinition();

            if (!_genericTypes.TryGetValue(genericType, out var genericMetaData))
            {
                throw new ArgumentException($"Generic type {genericType.FullName} has not been registered and cannot be used as a networked member");
            }

            //Supported generic type, create a concrete type
            var converter = genericMetaData.CreateConverter(this, type.GenericTypeArguments);

            return InternalRegisterType(type, TypeMetaData.DefaultFactory, converter, Array.Empty<TypeMetaData.Member>(), null);
        }

        internal TypeMetaData InternalRegisterType(Type type, TypeMetaData.ObjectFactory factory, ITypeConverter typeConverter, TypeMetaData.Member[] members, string mapFromType)
        {
            if (mapFromType != null)
            {
                //Verify that no other type maps from this type
                foreach (var typeMetaData in _types.Values)
                {
                    if ((typeMetaData.MapFromType != null && typeMetaData.MapFromType == mapFromType)
                        || typeMetaData.Type.FullName == mapFromType)
                    {
                        throw new NotSupportedException($"The type {type.FullName} maps from type {mapFromType}, but type {typeMetaData.Type.FullName} already maps from this type");
                    }
                }
            }

            if (_remappedTypes.ContainsKey(type))
            {
                throw new InvalidOperationException($"The type {type.FullName} has already been registered as a remapped type");
            }

            var metaData = new TypeMetaData(_nextId++, type, factory, typeConverter, members, mapFromType);

            _types.Add(type, metaData);

            return metaData;
        }

        /// <summary>
        /// Registers a type, adding all public networked fields and all public and non-public networked properties
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeConverter">Optional converter to use for this type</param>
        /// <param name="mapFromType"><see cref="TypeMetaDataBuilder.MapsFromType(string)"/></param>
        /// <returns></returns>
        public TypeMetaData RegisterType(Type type, ITypeConverter typeConverter = null, string mapFromType = null)
        {
            var builder = NewBuilder(type)
                .AddNetworkedFields()
                .AddNetworkedProperties();

            if (typeConverter != null)
            {
                builder.WithConverter(typeConverter);
            }

            if (mapFromType != null)
            {
                builder.MapsFromType(mapFromType);
            }

            return builder.Build();
        }

        public TypeMetaDataBuilder NewBuilder(Type type)
        {
            return new TypeMetaDataBuilder(this, type);
        }

        /// <summary>
        /// Registers a type that is mapped to another base type
        /// This allows a type to be networked as a base type
        /// </summary>
        /// <param name="remappedType"></param>
        /// <param name="inheritFrom"></param>
        public void RegisterRemappedType(Type remappedType, Type inheritFrom)
        {
            if (remappedType == null)
            {
                throw new ArgumentNullException(nameof(remappedType));
            }

            if (inheritFrom == null)
            {
                throw new ArgumentNullException(nameof(inheritFrom));
            }

            if (!inheritFrom.IsAssignableFrom(remappedType))
            {
                throw new InvalidOperationException($"The type {remappedType.FullName} does not inherit from {inheritFrom.FullName}");
            }

            var metaData = FindMetaDataByType(inheritFrom);

            if (metaData == null)
            {
                _remappedTypes.TryGetValue(inheritFrom, out metaData);
            }

            if (metaData == null)
            {
                throw new InvalidOperationException($"The type {inheritFrom.FullName} has not been registered");
            }

            _remappedTypes.Add(remappedType, metaData);
        }

        public NetworkObjectListObjectMetaDataList Serialize()
        {
            var list = new NetworkObjectListObjectMetaDataList();

            var sortedTypes = _types.Values.ToList();

            sortedTypes.Sort((lhs, rhs) => Comparer<uint>.Default.Compare(lhs.Id, rhs.Id));

            foreach (var type in sortedTypes)
            {
                var metaData = new ObjectMetaData
                {
                    TypeId = type.Id,
                    TypeName = type.Type.FullName
                };

                foreach (var member in type.Members)
                {
                    metaData.Members.Add(new ObjectMember
                    {
                        TypeId = member.MetaData.Id
                    });
                }

                list.MetaData.Add(metaData);
            }

            return list;
        }

        public void Deserialize(NetworkObjectListObjectMetaDataList list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            _transmitterToReceiverMap.Clear();

            //Construct a temporary map mapping our types by name so we can perform fast lookup
            var localLookup = _types.ToDictionary(
                entry => entry.Value.MapFromType ?? entry.Value.Type.FullName,
                entry => entry.Value);

            foreach (var metaData in list.MetaData)
            {
                if (!localLookup.TryGetValue(metaData.TypeName, out var type))
                {
                    throw new InvalidOperationException($"The type {metaData.TypeName} (id: {metaData.TypeId}) has no type mapping to it on the receiving end");
                }

                //Validate the type's members
                if (metaData.Members.Count != type.Members.Count)
                {
                    throw new InvalidOperationException(
                        $"The type {metaData.TypeName} (receiving mapping: {type.Type.FullName}) has a different number of members (transmitter: {metaData.Members.Count} receiver: {type.Members.Count})");
                }

                //the underlying type of a member can differ between transmitter and receiver depending on the converter, so don't try to match up the types

                //Add the lookup
                _transmitterToReceiverMap.Add(metaData.TypeId, type);
            }
        }
    }
}
