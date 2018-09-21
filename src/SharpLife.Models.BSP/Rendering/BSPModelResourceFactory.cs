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
using SharpLife.Game.Client.Renderer.Shared.Utility;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.BSP.Loading;
using SharpLife.Renderer;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Models.BSP.Rendering
{
    public sealed class BSPModelResourceFactory : IModelResourceFactory
    {
        public struct RenderArguments
        {
            public Vector4 RenderColor;

            public RenderMode RenderMode;

            public int Padding0;
            public int Padding1;
            public int Padding2;
        }

        //Must be sizeof(vec4) / sizeof(float) so it matches the buffer padding
        private static readonly int LightStylesElementMultiplier = Marshal.SizeOf<Vector4>() / Marshal.SizeOf<float>();

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private readonly int[] _cachedLightStyles = new int[BSPConstants.MaxLightStyles];

        public LightStyles LightStyles { get; }

        public ResourceLayout SharedLayout { get; private set; }
        public ResourceLayout TextureLayout { get; private set; }
        public ResourceLayout LightmapLayout { get; private set; }

        public RenderModePipelines Pipelines { get; private set; }

        public DeviceBuffer LightStylesBuffer { get; private set; }

        public DeviceBuffer RenderColorBuffer { get; private set; }

        public BSPModelResourceFactory(IRenderer renderer, LightStyles lightStyles)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            renderer.OnRenderBegin += OnRenderBegin;

            LightStyles = lightStyles ?? throw new ArgumentNullException(nameof(lightStyles));
        }

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

            //Reset the buffer so all styles will update in OnRenderBegin
            Array.Fill(_cachedLightStyles, LightStyles.InvalidLightValue);

            //Float arrays are handled by padding each element up to the size of a vec4,
            //so we have to account for the padding in creating and initializing the buffer
            var numLightStyleElements = BSPConstants.MaxLightStyles * LightStylesElementMultiplier;

            LightStylesBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)(numLightStyleElements * Marshal.SizeOf<int>()), BufferUsage.UniformBuffer));

            //Initialize the buffer to all zeroes
            var lightStylesValues = new float[numLightStyleElements];
            Array.Fill(lightStylesValues, 0.0f);
            gd.UpdateBuffer(LightStylesBuffer, 0, lightStylesValues);

            RenderColorBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<RenderArguments>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        }

        public void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();

            LightStylesBuffer = null;
            RenderColorBuffer = null;
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

        private void OnRenderBegin(IRenderer renderer, GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (LightStylesBuffer != null)
            {
                //Update the style buffer now, before anything is drawn
                for (var i = 0; i < BSPConstants.MaxLightStyles; ++i)
                {
                    var value = LightStyles.GetStyleValue(i);

                    if (_cachedLightStyles[i] != value)
                    {
                        _cachedLightStyles[i] = value;

                        //Index is multiplied here because of padding. See buffer creation code
                        gd.UpdateBuffer(LightStylesBuffer, (uint)(i * Marshal.SizeOf<float>() * LightStylesElementMultiplier), ref value);
                    }
                }
            }
        }
    }
}
