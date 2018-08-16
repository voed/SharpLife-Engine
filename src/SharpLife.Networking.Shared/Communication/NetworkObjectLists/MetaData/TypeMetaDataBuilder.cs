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
        private readonly TypeRegistry _registry;

        private bool _built;

        public Type Type { get; }

        private TypeMetaData.ObjectFactory _factory = TypeMetaData.DefaultFactory;

        private ITypeConverter _typeConverter;

        private readonly List<TypeMetaData.Member> _members = new List<TypeMetaData.Member>();

        private int _nextChangeNotificationIndex;

        private string _mapFromType;

        internal TypeMetaDataBuilder(TypeRegistry registry, Type type)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Type = type ?? throw new ArgumentNullException(nameof(type));

            if (type.IsGenericType && !type.IsConstructedGenericType)
            {
                throw new ArgumentException($"Type {type.FullName} is a generic type");
            }

            if (_registry.FindMetaDataByType(type) != null)
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

        private void InternalAddMember(MemberInfo memberInfo, Type type, bool usesChangeNotification)
        {
            var typeMetaData = _registry.LookupMemberType(type);

            if (typeMetaData == null)
            {
                throw new ArgumentException($"Type {type.FullName} has not been registered and is used as a networked member \"{memberInfo.Name}\" in {Type.FullName}", nameof(type));
            }

            _members.Add(new TypeMetaData.Member(memberInfo, typeMetaData, usesChangeNotification ? _nextChangeNotificationIndex++ : (int?)null));
        }

        public TypeMetaDataBuilder AddMember(string name, bool usesChangeNotification = false)
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

            InternalAddMember(memberInfo, type, usesChangeNotification);

            return this;
        }

        private TypeMetaDataBuilder InternalAddMemberInfo(MemberInfo info, Type type, bool usesChangeNotification)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (info.DeclaringType != Type)
            {
                throw new InvalidOperationException($"Member {info.Name} is not a member of type {Type.FullName}");
            }

            if (_members.FindIndex(member => member.Info == info) != -1)
            {
                throw new InvalidOperationException($"Member {info.Name} has already been added as a networked member for type {Type.FullName}");
            }

            InternalAddMember(info, type, usesChangeNotification);

            return this;
        }

        public TypeMetaDataBuilder AddMember(PropertyInfo propInfo, bool usesChangeNotification = false)
        {
            return InternalAddMemberInfo(propInfo, propInfo.PropertyType, usesChangeNotification);
        }

        public TypeMetaDataBuilder AddMember(FieldInfo fieldInfo, bool usesChangeNotification = false)
        {
            return InternalAddMemberInfo(fieldInfo, fieldInfo.FieldType, usesChangeNotification);
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
                var attr = member.GetCustomAttribute<NetworkedAttribute>();

                InternalAddMemberInfo(member, member.FieldType, attr.UsesChangeNotification);
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
                var attr = member.GetCustomAttribute<NetworkedAttribute>();

                InternalAddMemberInfo(member, member.PropertyType, attr.UsesChangeNotification);
            }

            return this;
        }

        /// <summary>
        /// Adds all public fields
        /// </summary>
        /// <returns></returns>
        public TypeMetaDataBuilder AddAllFields()
        {
            foreach (var member in Type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                AddMember(member, false);
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

            foreach (var member in Type.GetProperties(flags))
            {
                AddMember(member, false);
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
            return _registry.InternalRegisterType(Type, _factory, _typeConverter, _members.Count > 0 ? _members.ToArray() : Array.Empty<TypeMetaData.Member>(), _mapFromType);
        }
    }
}
