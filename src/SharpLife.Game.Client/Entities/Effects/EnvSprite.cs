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
using SharpLife.Game.Client.Renderer.Shared.Models.SPR;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models.SPR;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;

namespace SharpLife.Game.Client.Entities.Effects
{
    [Networkable]
    public class EnvSprite : NetworkedEntity
    {
        [Networked(TypeConverterType = typeof(FloatToIntConverter))]
        [BitConverterOptions(10, 4)]
        public float Frame { get; set; }

        [Networked]
        public float FrameRate { get; set; }

        public override void Render(IModelRenderer modelRenderer, IViewState viewState)
        {
            if (Model is SpriteModel spriteModel)
            {
                var renderData = new SpriteModelRenderData
                {
                    Model = spriteModel,
                    Shared = GetSharedModelRenderData(viewState),
                    Frame = Frame
                };

                modelRenderer.RenderSpriteModel(ref renderData);
            }
        }
    }
}
