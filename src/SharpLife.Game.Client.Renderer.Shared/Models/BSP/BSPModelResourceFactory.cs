﻿/***
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

using SharpLife.Game.Client.Renderer.Shared.Utility;
using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Models;
using System;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Models.BSP
{
    public sealed class BSPModelResourceFactory : IModelResourceFactory
    {
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public ResourceLayout SharedLayout { get; private set; }
        public ResourceLayout TextureLayout { get; private set; }
        public ResourceLayout LightmapLayout { get; private set; }

        public RenderModePipelines Pipelines { get; private set; }

        public DeviceBuffer RenderArgumentsBuffer { get; private set; }

        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            SharedLayout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightingInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightStyles", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("RenderColor", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            TextureLayout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            LightmapLayout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Lightmaps", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            var vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("LightmapCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    //Used for multiple light styles; this is the offset to apply to lightmap X coordinates
                    new VertexElementDescription("LightmapXOffset", VertexElementSemantic.Position, VertexElementFormat.Float1),
                    new VertexElementDescription("StyleIndices", VertexElementSemantic.Position, VertexElementFormat.Int4))
            };

            (var vs, var fs) = sc.MapResourceCache.GetShaders(gd, gd.ResourceFactory, "LightMappedGeneric");

            //Create render mode pipelines
            var rasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true);
            const PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList;
            var shaderSets = new ShaderSetDescription(vertexLayouts, new[] { vs, fs });
            var resourceLayouts = new ResourceLayout[] { SharedLayout, TextureLayout, LightmapLayout };
            var outputDescription = sc.MainSceneFramebuffer.OutputDescription;

            var pipelines = new Pipeline[(int)RenderMode.Last + 1];

            var pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.Normal] = disposeFactory.CreateGraphicsPipeline(ref pd);

            pipelines[(int)RenderMode.TransColor] = disposeFactory.CreateGraphicsPipeline(ref pd);

            pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqualRead : DepthStencilStateDescription.DepthOnlyLessEqualRead,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.TransTexture] = disposeFactory.CreateGraphicsPipeline(ref pd);

            //Glow uses the same pipeline as texture
            pipelines[(int)RenderMode.Glow] = pipelines[(int)RenderMode.TransTexture];

            pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.TransAlpha] = disposeFactory.CreateGraphicsPipeline(ref pd);

            var additiveBlend = new BlendStateDescription
            {
                AttachmentStates = new[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.One,
                        DestinationColorFactor = BlendFactor.One,
                        ColorFunction = BlendFunction.Add,
                        SourceAlphaFactor = BlendFactor.One,
                        DestinationAlphaFactor = BlendFactor.One,
                        AlphaFunction = BlendFunction.Add,
                    }
                }
            };

            pd = new GraphicsPipelineDescription(
                additiveBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqualRead : DepthStencilStateDescription.DepthOnlyLessEqualRead,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.TransAdd] = disposeFactory.CreateGraphicsPipeline(ref pd);

            Pipelines = new RenderModePipelines(pipelines);

            RenderArgumentsBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<BSPRenderArguments>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        }

        public void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();

            RenderArgumentsBuffer = null;
        }

        public void Dispose()
        {
            DestroyDeviceObjects(ResourceScope.All);
        }

        public ModelResourceContainer CreateContainer(IModel model)
        {
            if (!(model is BSPModel bspModel))
            {
                throw new ArgumentException("Model must be a BSP model", nameof(model));
            }

            return new BSPModelResourceContainer(this, bspModel);
        }
    }
}
