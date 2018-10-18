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
using SharpLife.Game.Client.Renderer.Shared.Models.BSP;
using SharpLife.Game.Client.Renderer.Shared.Models.MDL;
using SharpLife.Game.Client.Renderer.Shared.Models.SPR;
using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Game.Shared.Models.SPR;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System;
using System.Numerics;

namespace SharpLife.Game.Client.Entities
{
    [Networkable]
    public abstract class BaseEntity : SharedBaseEntity, IRenderableEntity
    {
        public EntityContext Context { get; set; }

        private Vector3 _origin;

        /// <summary>
        /// Gets the origin by reference
        /// Avoid using this
        /// </summary>
        public ref Vector3 RefOrigin => ref _origin;

        [Networked]
        public Vector3 Origin
        {
            get => _origin;
            set => _origin = value;
        }

        protected BaseEntity(bool networked)
            : base(networked)
        {
        }

        //Always call base first when overriding these
        public virtual void OnBeginUpdate()
        {
            //Nothing
        }

        public virtual void OnEndUpdate()
        {
            //Nothing
        }

        protected int CalculateFXBlend(IViewState viewState, int renderAmount)
        {
            //Offset is random based on entity index
            var offset = Handle.Id * 363.0f;

            int result;

            //Not all render effects update the render amount
            switch (RenderFX)
            {
                //All effects not handled use entity render amount (no special effect)
                default:
                    result = renderAmount;
                    break;

                //Pulsating transparency
                case RenderFX.PulseSlow:
                case RenderFX.PulseFast:
                case RenderFX.PulseSlowWide:
                case RenderFX.PulseFastWide:
                    {
                        var multiplier1 = (RenderFX == RenderFX.PulseSlow || RenderFX == RenderFX.PulseSlowWide) ? 2.0 : 8.0;
                        var multiplier2 = (RenderFX == RenderFX.PulseSlow || RenderFX == RenderFX.PulseFast) ? 16.0 : 64.0;
                        result = (int)Math.Floor(renderAmount + (Math.Sin(offset + (Context.Time.ElapsedTime * multiplier1)) * multiplier2));
                        break;
                    }

                //Fade out from solid to translucent
                case RenderFX.FadeSlow:
                case RenderFX.FadeFast:
                    result = renderAmount = Math.Max(0, renderAmount - (RenderFX == RenderFX.FadeSlow ? 1 : 4));
                    break;

                //Fade in from translucent to solid
                case RenderFX.SolidSlow:
                case RenderFX.SolidFast:
                    result = renderAmount = Math.Min(255, renderAmount + (RenderFX == RenderFX.SolidSlow ? 1 : 4));
                    break;

                //A strobing effect where the model becomes visible every so often
                case RenderFX.StrobeSlow:
                case RenderFX.StrobeFast:
                case RenderFX.StrobeFaster:
                    {
                        double multiplier;

                        switch (RenderFX)
                        {
                            case RenderFX.StrobeSlow:
                                multiplier = 4.0;
                                break;
                            case RenderFX.StrobeFast:
                                multiplier = 16.0;
                                break;
                            case RenderFX.StrobeFaster:
                                multiplier = 36.0;
                                break;

                            //Will never happen, silences compiler error
                            default: throw new InvalidOperationException("Update switch statement to handle render fx strobe cases");
                        }

                        if ((int)Math.Floor(Math.Sin(offset + (Context.Time.ElapsedTime * multiplier)) * 20.0) < 0)
                        {
                            return 0;
                        }

                        result = RenderAmount;
                        break;
                    }

                //Flicker in and out of existence
                case RenderFX.FlickerSlow:
                case RenderFX.FlickerFast:
                    {
                        double multiplier1;
                        double multiplier2;

                        if (RenderFX == RenderFX.FlickerSlow)
                        {
                            multiplier1 = 2.0;
                            multiplier2 = 17.0;
                        }
                        else
                        {
                            multiplier1 = 16.0;
                            multiplier2 = 23.0;
                        }

                        if ((int)Math.Floor(Math.Sin(offset * Context.Time.ElapsedTime * multiplier2) + (Math.Sin(Context.Time.ElapsedTime * multiplier1) * 20.0)) < 0)
                        {
                            return 0;
                        }

                        result = RenderAmount;
                        break;
                    }

                //Similar to pulse, but clamped to [148, 211], more chaotic
                case RenderFX.Distort:
                //Hologram effect based on player position and view direction relative to entity
                case RenderFX.Hologram:
                    {
                        int amount;
                        if (RenderFX == RenderFX.Distort)
                        {
                            amount = RenderAmount = 180;
                        }
                        else
                        {
                            var dot = Vector3.Dot(Origin - viewState.Origin, viewState.ViewVectors.Forward);

                            if (dot <= 0)
                            {
                                return 0;
                            }

                            RenderAmount = 180;

                            if (dot <= 100)
                            {
                                amount = 180;
                            }
                            else
                            {
                                amount = (int)Math.Floor((1 - ((dot - 100) * 0.0025)) * 180);
                            }
                        }
                        result = Context.Random.Next(-32, 31) + amount;
                        break;
                    }
            }

            return Math.Clamp(result, 0, 255);
        }

        protected SharedModelRenderData GetSharedModelRenderData(IViewState viewState)
        {
            var scale = Scale != 0 ? Scale : 1;

            //Normal behaves as though render amount is always 255
            var renderAmount = CalculateFXBlend(viewState, RenderMode != RenderMode.Normal ? RenderAmount : 255);

            return new SharedModelRenderData
            {
                Index = (uint)Handle.Id,

                Origin = Origin,
                Angles = Angles,
                Scale = new Vector3(scale),

                RenderFX = RenderFX,
                RenderMode = RenderMode,
                RenderAmount = renderAmount,
                RenderColor = RenderColor,

                Effects = Effects,
            };
        }

        public virtual void Render(IModelRenderer modelRenderer, IViewState viewState)
        {
            if (Model != null)
            {
                var sharedData = GetSharedModelRenderData(viewState);

                //Try to render models with as much data as possible
                switch (Model)
                {
                    case SpriteModel spriteModel:
                        {
                            var spriteRenderData = new SpriteModelRenderData
                            {
                                Model = spriteModel,
                                Shared = sharedData,
                            };
                            modelRenderer.RenderSpriteModel(ref spriteRenderData);
                            break;
                        }

                    case StudioModel studioModel:
                        {
                            var studioRenderData = new StudioModelRenderData
                            {
                                Model = studioModel,
                                Shared = sharedData,
                            };
                            modelRenderer.RenderStudioModel(ref studioRenderData);
                            break;
                        }

                    case BSPModel bspModel:
                        {
                            var brushRenderData = new BrushModelRenderData
                            {
                                Model = bspModel,
                                Shared = sharedData
                            };
                            modelRenderer.RenderBrushModel(ref brushRenderData);
                            break;
                        }

                    default: throw new InvalidOperationException($"Unknown model type {Model.GetType().FullName}");
                }
            }
        }
    }
}
