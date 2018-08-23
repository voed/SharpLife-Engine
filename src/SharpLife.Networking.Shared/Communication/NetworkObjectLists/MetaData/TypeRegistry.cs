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

using SharpLife.Networking.Shared.Messages.NetworkObjectLists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData
{
    /// <summary>
    /// Registry of networkable types
    /// </summary>
    public sealed class TypeRegistry
    {
        private readonly IReadOnlyDictionary<Type, TypeMetaData> _types;

        private readonly IReadOnlyDictionary<Type, TypeMetaData> _remappedTypes;

        private Dictionary<uint, TypeMetaData> _transmitterToReceiverMap;

        internal TypeRegistry(IReadOnlyDictionary<Type, TypeMetaData> types, IReadOnlyDictionary<Type, TypeMetaData> remappedTypes)
        {
            _types = types ?? throw new ArgumentNullException(nameof(types));
            _remappedTypes = remappedTypes ?? throw new ArgumentNullException(nameof(remappedTypes));
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

            _transmitterToReceiverMap = new Dictionary<uint, TypeMetaData>();

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
