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
using SharpLife.Game.Shared.Entities.MetaData.TypeConverters;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;

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

        [Networked(TypeConverterType = typeof(FrameTypeConverter))]
        public float Frame { get; set; }

        //TODO: may not need to be networked for studio models
        [Networked]
        public float FrameRate { get; set; }

        [Networked]
        public uint Body { get; set; }

        [Networked]
        public int Skin { get; set; }

        public override void Render(IModelRenderer modelRenderer, IViewState viewState)
        {
            if (Model is StudioModel studioModel)
            {
                var renderData = new StudioModelRenderData
                {
                    Model = studioModel,
                    Shared = GetSharedModelRenderData(viewState),
                    Sequence = Sequence,
                    Frame = Frame,
                    Body = Body,
                    Skin = Skin,
                };

                modelRenderer.RenderStudioModel(ref renderData);
            }
        }
    }
}
