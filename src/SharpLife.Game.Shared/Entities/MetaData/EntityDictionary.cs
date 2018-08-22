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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpLife.Game.Shared.Entities.MetaData
{
    /// <summary>
    /// Dictionary of entities
    /// </summary>
    public sealed class EntityDictionary
    {
        public sealed class EntityMetaData
        {
            public Type Type { get; }

            public string Name { get; }

            public EntityMetaData(Type type, string name)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }
        }

        private readonly Dictionary<string, EntityMetaData> _entityTypes = new Dictionary<string, EntityMetaData>();

        public IEnumerable<EntityMetaData> Types => _entityTypes.Values;

        public void AddTypesFromAssembly<TBaseEntity>(Assembly assembly)
            where TBaseEntity : class
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            foreach (var linkedEntity in assembly
                .GetTypes()
                .ToList()
                .Where(type => typeof(TBaseEntity).IsAssignableFrom(type)
                && type.GetCustomAttribute<LinkEntityToClassAttribute>() != null))
            {
                RegisterType(linkedEntity);
            }
        }

        private void RegisterType(Type type)
        {
            var link = type.GetCustomAttribute<LinkEntityToClassAttribute>();

            if (_entityTypes.ContainsKey(link.Name))
            {
                throw new InvalidOperationException($"Entity class \"{link.Name}\" already exists");
            }

            if (!type.IsClass || typeof(Delegate).IsAssignableFrom(type))
            {
                throw new NotSupportedException($"Entity class \"{link.Name}\"'s underlying type {type.FullName} is not a class");
            }

            //Detect missing constructor early
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new NotSupportedException($"Entity class \"{link.Name}\"'s underlying type {type.FullName} does not have a parameterless constructor, required to create instances");
            }

            var metaData = new EntityMetaData(type, link.Name);

            _entityTypes.Add(link.Name, metaData);
        }

        public EntityMetaData FindEntityMetaData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Entity name must be valid");
            }

            _entityTypes.TryGetValue(name, out var metaData);

            return metaData;
        }
    }
}
