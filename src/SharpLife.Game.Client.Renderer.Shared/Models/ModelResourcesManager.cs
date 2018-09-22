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

using SharpLife.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Game.Client.Renderer.Shared.Models
{
    public sealed class ModelResourcesManager : IModelResourcesManager, IEnumerable<ModelResourceContainer>
    {
        public delegate ModelResourceContainer ResourceFactory(IModel model);

        private IReadOnlyDictionary<Type, ResourceFactory> _resourceFactories;

        private readonly Dictionary<IModel, ModelResourceContainer> _containers = new Dictionary<IModel, ModelResourceContainer>();

        /// <summary>
        /// Creates a new resources manager that can create resources
        /// </summary>
        /// <param name="resourceFactories"></param>
        public ModelResourcesManager(IReadOnlyDictionary<Type, ResourceFactory> resourceFactories)
        {
            if (resourceFactories == null)
            {
                throw new ArgumentNullException(nameof(resourceFactories));
            }

            _resourceFactories = resourceFactories.ToDictionary(key => key.Key, element => element.Value);
        }

        public bool IsLoaded(IModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _containers.ContainsKey(model);
        }

        public ModelResourceContainer CreateResources(IModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (_containers.ContainsKey(model))
            {
                throw new ArgumentException($"Model \"{model.Name}\"'s resources have already been loaded", nameof(model));
            }

            if (!_resourceFactories.TryGetValue(model.GetType(), out var factory))
            {
                throw new InvalidOperationException($"Model type {model.GetType().FullName} is not supported");
            }

            var container = factory(model);

            _containers.Add(model, container);

            return container;
        }

        public ModelResourceContainer GetResources(IModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!_containers.TryGetValue(model, out var container))
            {
                throw new ArgumentException($"Model \"{model.Name}\"'s resources have not been loaded", nameof(model));
            }

            return container;
        }

        public void FreeAllResources()
        {
            foreach (var resource in _containers)
            {
                resource.Value.Dispose();
            }

            _containers.Clear();
        }

        public IEnumerator<ModelResourceContainer> GetEnumerator()
        {
            return _containers.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
