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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Entities.MetaData.TypeConverters;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Models;
using SharpLife.Models.MDL;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System.Diagnostics;

namespace SharpLife.Game.Server.Entities.Animation
{
    /// <summary>
    /// Base class for entities that use studio models
    /// </summary>
    [Networkable]
    public class BaseAnimating : NetworkedEntity
    {
        //TODO: it may be cheaper to just store a separate reference that updates whenever the model changes instead of casting
        public StudioModel StudioModel => Model as StudioModel;

        private uint _sequence;

        [Networked]
        public uint Sequence
        {
            get => _sequence;
            set
            {
                var studioModel = StudioModel;

                if (studioModel != null && value < studioModel.StudioFile.Sequences.Count)
                {
                    _sequence = value;
                    return;
                }

                _sequence = 0;
            }
        }

        [Networked(TypeConverterType = typeof(FrameTypeConverter))]
        public float Frame { get; set; }

        [Networked]
        public float FrameRate { get; set; }

        [Networked]
        public uint Body { get; set; }

        [Networked]
        public uint Skin { get; set; }

        //Must have a setter for networking purposes
        [Networked]
        public byte[] Controllers { get; set; } = new byte[MDLConstants.MaxControllers];

        protected override void OnModelChanged(IModel oldModel, IModel newModel)
        {
            //Reset model specific info
            Sequence = 0;
        }

        public override bool KeyValue(string key, string value)
        {
            if (key == "framerate")
            {
                FrameRate = KeyValueUtils.ParseFloat(value);
                return true;
            }

            return base.KeyValue(key, value);
        }

        public uint GetBodyGroup(uint group)
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                return StudioModelUtils.GetBodyGroupValue(studioModel.StudioFile, Body, group);
            }

            return 0;
        }

        public void SetBodyGroup(uint group, uint value)
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                Body = StudioModelUtils.CalculateBodyGroupValue(studioModel.StudioFile, Body, group, value);
            }
        }

        private float InternalSetBoneController(StudioFile studioFile, int controllerIndex, float value)
        {
            Debug.Assert(0 <= controllerIndex && controllerIndex < MDLConstants.MaxControllers);

            var result = StudioModelUtils.CalculateControllerValue(studioFile, controllerIndex, value, out value);

            if (result.HasValue)
            {
                Controllers[controllerIndex] = result.Value;
            }

            return value;
        }

        public float SetBoneController(int controllerIndex, float value)
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                value = InternalSetBoneController(studioModel.StudioFile, controllerIndex, value);
            }

            return value;
        }

        public void InitBoneControllers()
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                for (var i = 0; i < MDLConstants.MaxControllers; ++i)
                {
                    InternalSetBoneController(studioModel.StudioFile, i, 0.0f);
                }
            }
        }
    }
}
