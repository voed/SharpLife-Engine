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

using FastMember;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    public sealed class TypeMetaData
    {
        /// <summary>
        /// Delegate for object factories
        /// </summary>
        /// <param name="metaData">Metadata of the object type to create</param>
        /// <returns></returns>
        public delegate object ObjectFactory(TypeMetaData metaData);

        //Default to signalling to the user that this isn't implemented
        public static readonly ObjectFactory DefaultFactory = metaData => throw new NotSupportedException($"The type {metaData.Type.FullName} does not support instantiation");

        public sealed class Member
        {
            public MemberInfo Info { get; }

            public TypeMetaData MetaData { get; }

            public int? ChangeNotificationIndex { get; }

            public Member(MemberInfo info, TypeMetaData metaData, int? changeNotificationIndex)
            {
                Info = info ?? throw new ArgumentNullException(nameof(info));
                MetaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
                ChangeNotificationIndex = changeNotificationIndex;
            }
        }

        private readonly ObjectFactory _factory;

        private readonly Member[] _members;

        private readonly Dictionary<string, Member> _membersLookup = new Dictionary<string, Member>();

        public uint Id { get; }

        public Type Type { get; }

        /// <summary>
        /// The converter for this type
        /// If null, this type cannot be converted and cannot be used as a member in other types
        /// </summary>
        public ITypeConverter Converter { get; }

        public IReadOnlyList<Member> Members => _members;

        /// <summary>
        /// If not null, this is the transmitter's type that this type maps from
        /// </summary>
        public string MapFromType { get; }

        /// <summary>
        /// The number of members that have change notification
        /// </summary>
        public int ChangeNotificationMembersCount { get; }

        public TypeAccessor Accessor { get; }

        public TypeMetaData(uint id, Type type, ObjectFactory factory, ITypeConverter converter, Member[] members, string mapFromType)
        {
            Id = id;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Converter = converter;
            _members = members ?? throw new ArgumentNullException(nameof(members));

            MapFromType = mapFromType;

            ChangeNotificationMembersCount = _members.Count(member => member.ChangeNotificationIndex.HasValue);

            Accessor = TypeAccessor.Create(Type, true);
        }

        /// <summary>
        /// Creates an instance of the type
        /// </summary>
        /// <returns></returns>
        public object CreateInstance() => _factory(this);

        public Member FindMemberByName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_membersLookup.TryGetValue(name, out var member))
            {
                member = Array.Find(_members, arrayMember => arrayMember.Info.Name == name);

                if (member != null)
                {
                    //Use the info name to reduce the number of string instances
                    _membersLookup.Add(member.Info.Name, member);
                }
            }

            return member;
        }

        internal object[] AllocateSnapshot()
        {
            return new object[Members.Count];
        }
    }
}
