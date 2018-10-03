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
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Models;
using SharpLife.Models.MDL;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;
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
                }
                else
                {
                    _sequence = 0;
                }

                ResetSequenceInfo();
            }
        }

        //TODO: use a converter that transmits times relative to current time
        [Networked]
        public float LastTime { get; set; }

        [Networked(TypeConverterType = typeof(FloatToIntConverter))]
        [BitConverterOptions(10, 4)]
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

        [Networked]
        public byte[] Blenders { get; set; } = new byte[MDLConstants.MaxBlenders];

        [Networked]
        public int RenderFXLightMultiplier { get; set; }

        private float _lastEventCheck;

        public bool SequenceLoops { get; private set; }

        public bool SequenceFinished { get; protected set; }

        public float SequenceFrameRate { get; private set; }

        public float SeqenceGroundSpeed { get; private set; }

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

        public SequenceFlags GetSequenceFlags()
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                return StudioModelUtils.GetSequenceFlags(studioModel.StudioFile, Sequence);
            }

            return SequenceFlags.None;
        }

        public void ResetSequenceInfo()
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                StudioModelUtils.GetSequenceInfo(StudioModel.StudioFile, Sequence, out var sequenceFrameRate, out var groundSpeed);
                SequenceFrameRate = sequenceFrameRate;
                SeqenceGroundSpeed = groundSpeed;

                SequenceLoops = (GetSequenceFlags() & SequenceFlags.Looping) != 0;

                LastTime = (float)Context.Time.ElapsedTime;
                _lastEventCheck = (float)Context.Time.ElapsedTime;

                FrameRate = 1.0f;
                SequenceFinished = false;
            }
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

        public float SetBlending(int blenderIndex, float value)
        {
            var studioModel = StudioModel;

            if (studioModel != null)
            {
                var result = StudioModelUtils.CalculateBlendingValue(studioModel.StudioFile, Sequence, blenderIndex, value, out value);

                if (result.HasValue)
                {
                    Blenders[blenderIndex] = result.Value;
                }
            }

            return value;
        }

        /// <summary>
        /// advance the animation frame up to the current time
        /// if an flInterval is passed in, only advance animation that number of seconds
        /// </summary>
        /// <param name="flInterval"></param>
        /// <returns></returns>
        public float StudioFrameAdvance(float flInterval = 0.0f)
        {
            if (flInterval == 0.0)
            {
                flInterval = (float)(Context.Time.ElapsedTime - LastTime);

                if (flInterval <= 0.001)
                {
                    LastTime = (float)Context.Time.ElapsedTime;
                    return 0.0f;
                }
            }

            if (LastTime == 0)
            {
                flInterval = 0.0f;
            }

            Frame += flInterval * SequenceFrameRate * FrameRate;
            LastTime = (float)Context.Time.ElapsedTime;

            if (Frame < 0.0 || Frame >= 256.0)
            {
                if (SequenceLoops)
                {
                    Frame -= (int)(Frame / 256.0f) * 256.0f;
                }
                else
                {
                    Frame = (Frame < 0.0) ? 0 : 255;
                }

                SequenceFinished = true; // just in case it wasn't caught in GetEvents
            }

            return flInterval;
        }
    }
}
