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

using SharpLife.Game.Client.Renderer.Shared;
using SharpLife.Game.Client.Renderer.Shared.Models;
using SharpLife.Game.Client.Renderer.Shared.Models.MDL;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Models.MDL.Rendering;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;

namespace SharpLife.Game.Client.Entities.Animation
{
    /// <summary>
    /// Base class for entities that use studio models
    /// </summary>
    [Networkable]
    public class BaseAnimating : NetworkedEntity
    {
        [Networked]
        public uint Sequence { get; set; }

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

        public override void Render(IModelRenderer modelRenderer, IViewState viewState)
        {
            if (Model is StudioModel studioModel)
            {
                var renderData = new StudioModelRenderData
                {
                    Model = studioModel,
                    Shared = GetSharedModelRenderData(viewState),
                    CurrentTime = Context.Time.ElapsedTime,
                    Sequence = Sequence,
                    LastTime = LastTime,
                    Frame = Frame,
                    FrameRate = FrameRate,
                    Body = Body,
                    Skin = Skin,
                    BoneData = new BoneData(),
                    RenderFXLightMultiplier = RenderFXLightMultiplier,
                };

                for (var i = 0; i < MDLConstants.MaxControllers; ++i)
                {
                    renderData.BoneData.SetController(i, Controllers[i]);
                }

                for (var i = 0; i < MDLConstants.MaxBlenders; ++i)
                {
                    renderData.BoneData.SetBlender(i, Blenders[i]);
                }

                modelRenderer.RenderStudioModel(ref renderData);
            }
        }
    }
}
