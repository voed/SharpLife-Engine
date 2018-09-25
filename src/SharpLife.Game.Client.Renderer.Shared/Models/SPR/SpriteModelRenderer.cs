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

using Serilog;
using SharpLife.Game.Client.Renderer.Shared.Utility;
using SharpLife.Game.Shared.Models;
using SharpLife.Models.SPR.FileFormat;
using SharpLife.Models.SPR.Rendering;
using SharpLife.Renderer.Utility;
using SharpLife.Utility.Mathematics;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Models.SPR
{
    public sealed class SpriteModelRenderer : IResourceContainer
    {
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public ILogger Logger { get; }

        public ResourceLayout Layout { get; private set; }

        public RenderModePipelines Pipelines { get; private set; }

        public SpriteModelRenderer(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            //Create the layout and pipelines used by sprites

            var vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
                    )
            };

            (var vs, var fs) = sc.MapResourceCache.GetShaders(gd, gd.ResourceFactory, "SpriteGeneric");

            Layout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightingInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("RenderColor", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                ));

            //Vanilla GoldSource has a cvar gl_spriteblend that disables blending and makes normal sprites render differently
            //This is purely a performance setting and lowers the quality of sprites
            //We won't do that here
            var pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { Layout },
                sc.MainSceneFramebuffer.OutputDescription);

            var pipelines = new Pipeline[(int)RenderMode.Last + 1];

            pipelines[(int)RenderMode.Normal] = disposeFactory.CreateGraphicsPipeline(ref pd);

            //Same pipeline as normal when sprite blending is enabled in vanilla
            pipelines[(int)RenderMode.TransTexture] = pipelines[(int)RenderMode.Normal];

            //Identical to Texture, vanilla GoldSource uses an invalid texture env mode that happens to use GL_MODULATE so for consistency this is required
            pipelines[(int)RenderMode.TransColor] = pipelines[(int)RenderMode.TransTexture];

            pd = new GraphicsPipelineDescription(
                BlendStates.SingleAdditiveOneOneBlend,
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { Layout },
                sc.MainSceneFramebuffer.OutputDescription);

            pipelines[(int)RenderMode.Glow] = disposeFactory.CreateGraphicsPipeline(ref pd);

            //Identical to Texture, but does not write to the depth buffer
            pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqualRead : DepthStencilStateDescription.DepthOnlyLessEqualRead,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { Layout },
                sc.MainSceneFramebuffer.OutputDescription);

            pipelines[(int)RenderMode.TransAlpha] = disposeFactory.CreateGraphicsPipeline(ref pd);

            //Identical to Glow, but still uses depth testing
            pd = new GraphicsPipelineDescription(
                BlendStates.SingleAdditiveOneOneBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqualRead : DepthStencilStateDescription.DepthOnlyLessEqualRead,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { Layout },
                sc.MainSceneFramebuffer.OutputDescription);

            pipelines[(int)RenderMode.TransAdd] = disposeFactory.CreateGraphicsPipeline(ref pd);

            Pipelines = new RenderModePipelines(pipelines);
        }

        public void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
        }

        public void Dispose()
        {
            DestroyDeviceObjects(ResourceScope.All);
        }

        private Vector3 CalculateSpriteColor(ref SharedModelRenderData renderData, int alpha)
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

        private int GlowBlend(ref SharedModelRenderData renderData, IViewState viewState)
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

        private Vector3 GetSpriteAngles(ref SharedModelRenderData renderData, SpriteType type, IViewState viewState)
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
                        Logger.Warning($"{nameof(GetSpriteAngles)}: Bad sprite type {type}");
                        break;
                    }
            }

            return new Vector3();
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, SpriteModelResourceContainer modelResource, ref SpriteModelRenderData renderData)
        {
            if (renderData.Shared.RenderMode == RenderMode.Glow)
            {
                renderData.Shared.RenderAmount = GlowBlend(ref renderData.Shared, sc.ViewState);
            }

            //Don't render if blend is 0 (even if blend were ignored below)
            if (renderData.Shared.RenderAmount == 0)
            {
                return;
            }

            var blend = renderData.Shared.RenderMode != RenderMode.Normal ? renderData.Shared.RenderAmount : 255;

            //TODO: glow sprite visibility testing
            var angles = GetSpriteAngles(ref renderData.Shared, modelResource.SpriteModel.SpriteFile.Type, sc.ViewState);

            angles = VectorUtils.ToRadians(angles);

            var wai = new WorldAndInverse();

            var anglesWithoutYaw = angles;

            anglesWithoutYaw.Y = 0;

            wai.World = Matrix4x4.CreateScale(renderData.Shared.Scale)
                * WorldAndInverse.CreateRotationMatrix(anglesWithoutYaw)
                * WorldAndInverse.CreateRotationMatrix(new Vector3(0, angles.Y, 0))
                * Matrix4x4.CreateTranslation(renderData.Shared.Origin);

            wai.InverseWorld = VdUtilities.CalculateInverseTranspose(ref wai.World);

            //var wai = new WorldAndInverse(renderData.Origin, angles, renderData.Scale);

            sc.UpdateWorldAndInverseBuffer(cl, ref wai);

            var alpha = 255;

            switch (renderData.Shared.RenderMode)
            {
                case RenderMode.Normal:
                case RenderMode.TransTexture:
                case RenderMode.TransAlpha:
                    alpha = blend;
                    break;
            }

            var renderColor = new Vector4(CalculateSpriteColor(ref renderData.Shared, blend), alpha) / 255.0f;

            cl.UpdateBuffer(modelResource.RenderColorBuffer, 0, ref renderColor);

            renderData.Frame = Math.Clamp((int)renderData.Frame, 0, modelResource.VertexBuffers.Length - 1);

            var frameBuffer = modelResource.VertexBuffers[(int)renderData.Frame];

            var pipeline = Pipelines[renderData.Shared.RenderMode];

            cl.SetVertexBuffer(0, frameBuffer);
            cl.SetIndexBuffer(modelResource.IndexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, modelResource.ResourceSet);
            cl.DrawIndexed((uint)SPRResourceUtils.Indices.Length, 1, 0, 0, 0);
        }
    }
}
