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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    /// <summary>
    /// Provides a way to build a type's metadata
    /// </summary>
    public sealed class TypeMetaDataBuilder
    {
        private readonly TypeRegistryBuilder _registryBuilder;

        private bool _built;

        public Type Type { get; }

        private TypeMetaData.ObjectFactory _factory = TypeMetaData.DefaultFactory;

        private ITypeConverter _typeConverter;

        private readonly List<TypeMetaData.Member> _members = new List<TypeMetaData.Member>();

        private int _nextChangeNotificationIndex;

        private string _mapFromType;

        internal TypeMetaDataBuilder(TypeRegistryBuilder registryBuilder, Type type)
        {
            _registryBuilder = registryBuilder ?? throw new ArgumentNullException(nameof(registryBuilder));
            Type = type ?? throw new ArgumentNullException(nameof(type));

            if (type.IsGenericType && !type.IsConstructedGenericType)
            {
                throw new ArgumentException($"Type {type.FullName} is a generic type");
            }

            if (_registryBuilder.FindMetaDataByType(type) != null)
            {
                throw new ArgumentException($"Type {type.FullName} has already been registered");
            }
        }

        ~TypeMetaDataBuilder()
        {
            if (!_built)
            {
                throw new InvalidOperationException($"Type builder for {Type.FullName} has not been built");
            }
        }

        /// <summary>
        /// Provide a factory to instantiate root types with
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        public TypeMetaDataBuilder WithFactory(TypeMetaData.ObjectFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            return this;
        }

        public TypeMetaDataBuilder WithConverter(ITypeConverter typeConverter)
        {
            if (_typeConverter != null)
            {
                throw new ArgumentException($"Type converter for type {Type.FullName} already provided");
            }

            _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));

            return this;
        }

        private void InternalAddMember(MemberInfo memberInfo, Type type, ITypeConverter typeConverter, in BitConverterOptions converterOptions, bool usesChangeNotification)
        {
            var typeMetaData = _registryBuilder.LookupMemberType(type);

            if (typeMetaData == null)
            {
                throw new ArgumentException($"Type {type.FullName} has not been registered and is used as a networked member \"{memberInfo.Name}\" in {Type.FullName}", nameof(type));
            }

            //Fall back to using the default converter for the type, if there is one
            if (typeConverter == null)
            {
                typeConverter = typeMetaData.Converter;
            }

            //Convert the converter options to the most optimal format now
            var optimizedConverterOptions = typeConverter?.OptimizeOptions(converterOptions) ?? converterOptions;

            _members.Add(new TypeMetaData.Member(memberInfo, typeMetaData, typeConverter, optimizedConverterOptions, usesChangeNotification ? _nextChangeNotificationIndex++ : (int?)null));
        }

        /// <summary>
        /// Adds a member
        /// </summary>
        /// <param name="name"></param>
        /// <param name="typeConverter">If specified, this converter will be used instead of the default for the member type</param>
        /// <param name="converterOptions"></param>
        /// <param name="usesChangeNotification"></param>
        /// <returns></returns>
        public TypeMetaDataBuilder AddMember(string name,
            ITypeConverter typeConverter,
            BitConverterOptions converterOptions,
            bool usesChangeNotification = false)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_members.FindIndex(member => member.Info.Name == name) != -1)
            {
                throw new InvalidOperationException($"Member {name} has already been added as a networked member for type {Type.FullName}");
            }

            MemberInfo memberInfo = Type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Type type;

            if (memberInfo == null)
            {
                //FastMember only supports public field access
                memberInfo = Type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

                if (memberInfo == null)
                {
                    throw new ArgumentException($"Member {name} does not exist in type {Type.FullName}");
                }

                type = ((FieldInfo)memberInfo).FieldType;
            }
            else
            {
                type = ((PropertyInfo)memberInfo).PropertyType;
            }

            InternalAddMember(memberInfo, type, typeConverter, converterOptions, usesChangeNotification);

            return this;
        }

        private TypeMetaDataBuilder InternalAddMemberInfo(MemberInfo info, Type type, ITypeConverter typeConverter, in BitConverterOptions converterOptions, bool usesChangeNotification)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            //Account for base classes
            if (!info.DeclaringType.IsAssignableFrom(Type))
            {
                throw new InvalidOperationException($"Member {info.Name} is not a member of type {Type.FullName}");
            }

            if (_members.FindIndex(member => member.Info == info) != -1)
            {
                throw new InvalidOperationException($"Member {info.Name} has already been added as a networked member for type {Type.FullName}");
            }

            InternalAddMember(info, type, typeConverter, converterOptions, usesChangeNotification);

            return this;
        }

        private TypeMetaDataBuilder InternalAddMemberInfo(MemberInfo info, Type type, Type typeConverterType, in BitConverterOptions converterOptions, bool usesChangeNotification)
        {
            ITypeConverter typeConverter = null;

            //Try to create the converter if provided
            if (typeConverterType != null)
            {
                typeConverter = (ITypeConverter)Activator.CreateInstance(typeConverterType);
            }

            return InternalAddMemberInfo(info, type, typeConverter, converterOptions, usesChangeNotification);
        }

        public TypeMetaDataBuilder AddMember(PropertyInfo propInfo, ITypeConverter typeConverter, in BitConverterOptions converterOptions, bool usesChangeNotification = false)
        {
            return InternalAddMemberInfo(propInfo, propInfo.PropertyType, typeConverter, converterOptions, usesChangeNotification);
        }

        public TypeMetaDataBuilder AddMember(FieldInfo fieldInfo, ITypeConverter typeConverter, in BitConverterOptions converterOptions, bool usesChangeNotification = false)
        {
            return InternalAddMemberInfo(fieldInfo, fieldInfo.FieldType, typeConverter, converterOptions, usesChangeNotification);
        }

        private void InternalAddNetworkedMember(MemberInfo info, Type type)
        {
            var networkedAttr = info.GetCustomAttribute<NetworkedAttribute>();

            var bitConverterAttr = info.GetCustomAttribute<BitConverterOptionsAttribute>();

            var converterOptions = bitConverterAttr?.Options ?? BitConverterOptions.Default;

            InternalAddMemberInfo(info, type, networkedAttr.TypeConverterType, converterOptions, networkedAttr.UsesChangeNotification);
        }

        /// <summary>
        /// Adds all fields marked with <see cref="NetworkedAttribute"/>
        /// </summary>
        /// <returns></returns>
        public TypeMetaDataBuilder AddNetworkedFields()
        {
            foreach (var member in Type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member.GetCustomAttribute<NetworkedAttribute>() != null))
            {
                InternalAddNetworkedMember(member, member.FieldType);
            }

            return this;
        }

        /// <summary>
        /// Adds all properties marked with <see cref="NetworkedAttribute"/>
        /// </summary>
        /// <returns></returns>
        public TypeMetaDataBuilder AddNetworkedProperties()
        {
            foreach (var member in Type
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member.GetCustomAttribute<NetworkedAttribute>() != null))
            {
                InternalAddNetworkedMember(member, member.PropertyType);
            }

            return this;
        }

        /// <summary>
        /// Adds all public fields
        /// </summary>
        /// <returns></returns>
        public TypeMetaDataBuilder AddAllFields()
        {
            //TODO: check for NetworkedAttribute?
            foreach (var member in Type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                AddMember(member, null, BitConverterOptions.Default, false);
            }

            return this;
        }

        /// <summary>
        /// Adds all properties
        /// </summary>
        /// <param name="nonPublic">If true, non-public properties will also be added</param>
        /// <returns></returns>
        public TypeMetaDataBuilder AddAllProperties(bool nonPublic)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            if (nonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            //TODO: check for NetworkedAttribute?
            foreach (var member in Type.GetProperties(flags))
            {
                AddMember(member, null, BitConverterOptions.Default, false);
            }

            return this;
        }

        /// <summary>
        /// For types that have a different data type on the receiving end, this lets them specify that they map from a specific sending type
        /// </summary>
        /// <param name="mapFromType"></param>
        /// <returns></returns>
        public TypeMetaDataBuilder MapsFromType(string mapFromType)
        {
            if (mapFromType == null)
            {
                throw new ArgumentNullException(nameof(mapFromType));
            }

            if (mapFromType.All(char.IsWhiteSpace))
            {
                throw new ArgumentException("Map from type must be a valid name", nameof(mapFromType));
            }

            _mapFromType = mapFromType;

            return this;
        }

        public TypeMetaData Build()
        {
            if (_built)
            {
                throw new InvalidOperationException($"Type builder for {Type.FullName} has already been built");
            }

            _built = true;

            //Avoid unnecessary memory allocations
            return _registryBuilder.InternalRegisterType(Type, _factory, _typeConverter, _members.Count > 0 ? _members.ToArray() : Array.Empty<TypeMetaData.Member>(), _mapFromType);
        }
    }
}
