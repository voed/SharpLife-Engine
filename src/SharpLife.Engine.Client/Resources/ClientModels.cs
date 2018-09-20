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
using SharpLife.Engine.Shared.Networking;
using SharpLife.Models;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;
using System;
using System.Collections.Generic;

namespace SharpLife.Engine.Client.Resources
{
    internal sealed class ClientModels : IEngineModels
    {
        private readonly IModelManager _modelManager;

        private INetworkStringList _models;

        //Maps model indices to their model instances
        //Negative values are client side model indices
        private Dictionary<int, IModel> _loadedModels;

        private Dictionary<string, int> _nameToIndex;

        private int _nextClientIndex;

        public ClientModels(IModelManager modelManager)
        {
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
        }

        internal void CreateNetworkStringLists(INetworkStringListsBuilder networkStringListBuilder)
        {
            _models = networkStringListBuilder.CreateList(EngineStringLists.Models);

            _models.OnStringAdded += _modelPrecache_OnStringAdded;

            _loadedModels = new Dictionary<int, IModel>();
            _nameToIndex = new Dictionary<string, int>();

            _nextClientIndex = -1;
        }

        private void _modelPrecache_OnStringAdded(IReadOnlyNetworkStringList list, int index)
        {
            var data = list.GetBinaryData(index) as ModelPrecacheData;

            /*var model = */

            //Load the model now
            InternalLoadModel(list[index], NetUtilities.ConvertToPlatformPath(list[index]), index);

            //TODO: need to implement consistency checking and enforce models if requested by server
            /*
            if (model == null && ((ModelPrecacheFlags)data.Flags & ModelPrecacheFlags.Required) != 0)
            {
                //TODO: use a new exception type, handle error higher up
                throw new InvalidOperationException($"Cannot continue without \"{list[index]}\", disconnecting");
            }
            */
        }

        private int InternalLoadModel(string networkModelName, string modelName, int? serverIndex)
        {
            var model = _modelManager.Load(modelName);

            int index;

            if (serverIndex.HasValue)
            {
                index = serverIndex.Value;
            }
            else
            {
                //Should never happen
                if (_nextClientIndex == int.MinValue)
                {
                    throw new NotSupportedException($"Cannot load any more models on the client side (maximum: {Math.Abs((long)int.MinValue)})");
                }

                index = _nextClientIndex--;
            }

            _loadedModels.Add(index, model);
            _nameToIndex.Add(networkModelName, index);

            return index;
        }

        public IModel LoadModel(string modelName)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            if (_models == null)
            {
                throw new InvalidOperationException($"Cannot load model \"{modelName}\"; network string list not created yet");
            }

            var platformModelName = NetUtilities.ConvertToPlatformPath(modelName);
            var networkModelName = NetUtilities.ConvertToNetworkPath(modelName);

            if (!_nameToIndex.TryGetValue(networkModelName, out var index))
            {
                //See if it's a server loaded model
                index = _models.IndexOf(networkModelName);

                index = InternalLoadModel(networkModelName, platformModelName, index != -1 ? index : (int?)null);
            }

            return _modelManager[platformModelName];
        }

        public IModel GetModel(in ModelIndex index)
        {
            if (index.Valid)
            {
                var internalIndex = ModelUtils.GetInternalIndex(index);

                if (_loadedModels.TryGetValue(internalIndex, out var model))
                {
                    return model;
                }

                throw new ArgumentException($"Model index {internalIndex} is invalid", nameof(index));
            }

            return null;
        }

        public ModelIndex IndexOf(IModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (_nameToIndex.TryGetValue(NetUtilities.ConvertToNetworkPath(model.Name), out var index))
            {
                return ModelUtils.CreateModelIndex(index);
            }

            return new ModelIndex();
        }

        public bool IndicesEqual(in ModelIndex lhs, in ModelIndex rhs)
        {
            //Positive values are server networked
            //Don't need to check if rhs is positive, the check below will handle it
            if (lhs.Index >= 0)
            {
                return lhs == rhs;
            }

            //The server can load a model after the client has loaded it, in which case we'll need to make sure the models match
            var lhsModel = GetModel(lhs);
            var rhsModel = GetModel(rhs);

            return ReferenceEquals(lhsModel, rhsModel);
        }
    }
}
