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
using SharpLife.Models.SPR.FileFormat;
using SharpLife.Models.SPR.Loading;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using SharpLife.Utility.Mathematics;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Utilities;

namespace SharpLife.Models.SPR.Rendering
{
    public class SpriteModelResourceContainer : ModelResourceContainer
    {
        private readonly SpriteModelResourceFactory _factory;

        private readonly SpriteModel _spriteModel;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private DeviceBuffer[] _frameBuffers;
        private DeviceBuffer _ib;
        private DeviceBuffer _renderColorBuffer;
        private ResourceSet _resourceSet;

        public override IModel Model => _spriteModel;

        public SpriteModelResourceContainer(SpriteModelResourceFactory factory, SpriteModel spriteModel)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _spriteModel = spriteModel ?? throw new ArgumentNullException(nameof(spriteModel));
        }

        private Vector3 CalculateSpriteColor(ref ModelRenderData renderData, int alpha)
        {
            var colorMultiplier = renderData.RenderMode == RenderMode.Glow || renderData.RenderMode == RenderMode.TransAdd ? alpha : 256;

            //TODO: this doesn't handle filter colors
            if (renderData.RenderColor == Vector3.Zero)
            {
                return new Vector3(colorMultiplier * 255 / 256);
            }

            var color = renderData.RenderColor;

            //This replicates a bug in the engine that causes Color render mode to ignore render mode
            //This way the color from the sprite is retained
            if (renderData.RenderMode == RenderMode.TransColor)
            {
                color = Vector3.One * 255;
            }

            color *= colorMultiplier;
            color /= 256.0f;

            //TODO: handle filtering
            //TODO: should probably be a post processing effect
            /*
            if (filterMode)
            {
                color *= filterColor;
            }
            */

            return color;
        }

        private int GlowBlend(ref ModelRenderData renderData, IViewState viewState)
        {
            var tmp = renderData.Origin - viewState.Origin;

            var distance = tmp.Length();

            //TODO: implement trace

            /*
            pmove->usehull = 2;

            var traceFlags = PM_GLASS_IGNORE;

            if (!r_traceglow.value)
            {
                traceFlags |= PM_STUDIO_IGNORE;
            }

            var trace = pmove->PM_PlayerTrace(viewState.Origin, renderData.Origin, traceFlags, -1);

            if ((1.0 - trace.fraction) * distance > 8.0)
            {
                return 0;
            }
            else */
            if (renderData.RenderFX == RenderFX.NoDissipation)
            {
                return renderData.RenderAmount;
            }
            else
            {
                renderData.Scale = new Vector3(distance * 0.005f);

                return (int)(Math.Clamp(19000.0f / (distance * distance), 0.5f, 1.0f) * 255);
            }
        }

        private Vector3 GetSpriteAngles(ref ModelRenderData renderData, SpriteType type, IViewState viewState)
        {
            //Convert parallel sprites to parallel oriented if a roll was specified
            if (type == SpriteType.Parallel && renderData.Angles.Z != 0)
            {
                type = SpriteType.ParallelOriented;
            }

            Vector3 GetModifiedViewAngles()
            {
                var angles = viewState.Angles;

                //Pitch and roll need to be inverted to operate in the sprite's coordinate system
                //Yaw stays the same
                angles.X = -angles.X;
                angles.Z = -angles.Z;

                return angles;
            }

            switch (type)
            {
                case SpriteType.FacingUpright:
                    {
                        //This is bugged in vanilla since it uses an origin that isn't initialized by sprite models, only other models
                        var angles = VectorUtils.VectorToAngles(-renderData.Origin);

                        //No pitch
                        angles.X = 0;

                        return angles;
                    }

                case SpriteType.ParallelUpright:
                    {
                        var angles = GetModifiedViewAngles();

                        //No pitch
                        angles.X = 0;

                        return angles;
                    }

                case SpriteType.Parallel:
                    {
                        return GetModifiedViewAngles();
                    }

                case SpriteType.Oriented:
                    {
                        return renderData.Angles;
                    }

                case SpriteType.ParallelOriented:
                    {
                        var angles = GetModifiedViewAngles();

                        //Apply roll
                        angles.Z -= renderData.Angles.Z;

                        return angles;
                    }

                default:
                    {
                        _factory.Logger.Warning($"{nameof(GetSpriteAngles)}: Bad sprite type {type}");
                        break;
                    }
            }

            return new Vector3();
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
            if (renderData.RenderMode == RenderMode.Glow)
            {
                renderData.RenderAmount = GlowBlend(ref renderData, sc.ViewState);
            }

            //Don't render if blend is 0 (even if blend were ignored below)
            if (renderData.RenderAmount == 0)
            {
                return;
            }

            var blend = renderData.RenderMode != RenderMode.Normal ? renderData.RenderAmount : 255;

            //TODO: glow sprite visibility testing
            var angles = GetSpriteAngles(ref renderData, _spriteModel.SpriteFile.Type, sc.ViewState);

            angles = VectorUtils.ToRadians(angles);

            var wai = new WorldAndInverse();

            var anglesWithoutYaw = angles;

            anglesWithoutYaw.Y = 0;

            wai.World = Matrix4x4.CreateScale(renderData.Scale)
                * WorldAndInverse.CreateRotationMatrix(anglesWithoutYaw)
                * WorldAndInverse.CreateRotationMatrix(new Vector3(0, angles.Y, 0))
                * Matrix4x4.CreateTranslation(renderData.Origin);

            wai.InverseWorld = VdUtilities.CalculateInverseTranspose(ref wai.World);

            //var wai = new WorldAndInverse(renderData.Origin, angles, renderData.Scale);

            sc.UpdateWorldAndInverseBuffer(cl, ref wai);

            var alpha = 255;

            switch (renderData.RenderMode)
            {
                case RenderMode.Normal:
                case RenderMode.TransTexture:
                case RenderMode.TransAlpha:
                    alpha = blend;
                    break;
            }

            var renderColor = new Vector4(CalculateSpriteColor(ref renderData, blend), alpha) / 255.0f;

            cl.UpdateBuffer(_renderColorBuffer, 0, ref renderColor);

            renderData.Frame = Math.Clamp((int)renderData.Frame, 0, _frameBuffers.Length - 1);

            var frameBuffer = _frameBuffers[(int)renderData.Frame];

            var pipeline = _factory.Pipelines[renderData.RenderMode];

            cl.SetVertexBuffer(0, frameBuffer);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, _resourceSet);
            cl.DrawIndexed((uint)SPRResourceUtils.Indices.Length, 1, 0, 0, 0);
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            (var atlasImage, var frameBuffers) = SPRResourceUtils.CreateSpriteAtlas(_spriteModel.SpriteFile, gd, disposeFactory, sc.TextureLoader);

            //TODO: disable mipmapping for certain sprites?
            var texture = sc.MapResourceCache.AddTexture2D(gd, disposeFactory, new ImageSharpTexture(atlasImage, true), $"{_spriteModel.Name}_atlas");

            _frameBuffers = frameBuffers;

            _ib = SPRResourceUtils.CreateIndexBuffer(gd, disposeFactory);

            _renderColorBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<Vector4>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var view = sc.MapResourceCache.GetTextureView(gd.ResourceFactory, texture);

            _resourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _factory.Layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                sc.WorldAndInverseBuffer,
                view,
                sc.MainSampler,
                sc.LightingInfoBuffer,
                _renderColorBuffer));
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
            _frameBuffers = null;
        }
    }
}
