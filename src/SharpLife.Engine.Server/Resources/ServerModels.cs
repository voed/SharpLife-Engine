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

using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Engine.Shared.Networking;
using SharpLife.Models;
using SharpLife.Networking.Shared;
using SharpLife.Networking.Shared.Communication.NetworkStringLists;
using SharpLife.Networking.Shared.Messages.Server;
using SharpLife.Networking.Shared.Precaching;
using System;

namespace SharpLife.Engine.Server.Resources
{
    internal sealed class ServerModels : IServerModels
    {
        private readonly IModelManager _modelManager;

        private readonly string _fallbackModelName;

        private INetworkStringList _models;

        public ServerModels(IModelManager modelManager, string fallbackModelName)
        {
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _fallbackModelName = fallbackModelName ?? throw new ArgumentNullException(nameof(fallbackModelName));
        }

        internal void CreateNetworkStringLists(INetworkStringListsBuilder networkStringListBuilder)
        {
            _models = networkStringListBuilder.CreateList(EngineStringLists.Models);
        }

        public void LoadFallbackModel()
        {
            //The manager needs to know that the fallback model is loaded, so this has to be called explicitly
            _modelManager.LoadFallbackModel(NetUtilities.ConvertToPlatformPath(_fallbackModelName));

            //Add it to our list
            LoadModel(_fallbackModelName);
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
            //TODO: could rework string lists to internally handle this at some point
            var networkModelName = NetUtilities.ConvertToNetworkPath(modelName);

            var index = _models.IndexOf(networkModelName);

            if (index == -1)
            {
                var model = _modelManager.Load(platformModelName);

                //All models loaded by the server are required by default
                index = _models.Add(networkModelName, new ModelPrecacheData
                {
                    Flags = (uint)ModelPrecacheFlags.Required
                });
            }

            return _modelManager[platformModelName];
        }

        public IModel GetModel(in ModelIndex index)
        {
            if (index.Valid)
            {
                var internalIndex = ModelUtils.GetInternalIndex(index);

                if (internalIndex < 0 || internalIndex >= _models.Count)
                {
                    throw new ArgumentException($"Model index {internalIndex} is invalid", nameof(index));
                }

                return _modelManager[NetUtilities.ConvertToPlatformPath(_models[internalIndex])];
            }

            return null;
        }

        public ModelIndex IndexOf(IModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var index = _models.IndexOf(NetUtilities.ConvertToNetworkPath(model.Name));

            if (index != -1)
            {
                return ModelUtils.CreateModelIndex(index);
            }

            return new ModelIndex();
        }

        public bool IndicesEqual(in ModelIndex lhs, in ModelIndex rhs)
        {
            //The server can just compare the values directly
            return lhs == rhs;
        }
    }
}
