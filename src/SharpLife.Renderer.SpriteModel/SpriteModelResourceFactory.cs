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
using SharpLife.Models;
using SharpLife.Renderer.Models;
using SharpLife.Renderer.Utility;
using System;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer.SpriteModel
{
    public sealed class SpriteModelResourceFactory : IModelResourceFactory
    {
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public ILogger Logger { get; }

        public ResourceLayout Layout { get; private set; }

        public RenderModePipelines Pipelines { get; private set; }

        public SpriteModelResourceFactory(ILogger logger)
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

            //Additive blend that matches glBlendFunc(GL_ONE, GL_ONE)
            var additiveBlend = new BlendStateDescription
            {
                AttachmentStates = new BlendAttachmentDescription[]
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
                additiveBlend,
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

        public ModelResourceContainer CreateContainer(IModel model)
        {
            if (!(model is SharpLife.Models.SPR.SpriteModel spriteModel))
            {
                throw new ArgumentException("Model must be a sprite model", nameof(model));
            }

            return new SpriteModelResourceContainer(this, spriteModel);
        }
    }
}
