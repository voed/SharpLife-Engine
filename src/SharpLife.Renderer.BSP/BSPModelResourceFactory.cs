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

using SharpLife.FileFormats.BSP;
using SharpLife.Models;
using SharpLife.Models.BSP;
using SharpLife.Renderer.Models;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer.BSP
{
    public sealed class BSPModelResourceFactory : IModelResourceFactory
    {
        //Must be sizeof(vec4) / sizeof(float) so it matches the buffer padding
        private static readonly int LightStylesElementMultiplier = Marshal.SizeOf<Vector4>() / Marshal.SizeOf<float>();

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private readonly int[] _cachedLightStyles = new int[BSPConstants.MaxLightStyles];

        public LightStyles LightStyles { get; }

        public ResourceLayout SharedLayout { get; private set; }
        public ResourceLayout TextureLayout { get; private set; }
        public ResourceLayout LightmapLayout { get; private set; }

        public Pipeline Pipeline { get; private set; }

        public DeviceBuffer LightStylesBuffer { get; private set; }

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
                new ResourceLayoutElementDescription("LightStyles", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

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

            var pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { SharedLayout, TextureLayout, LightmapLayout },
                sc.MainSceneFramebuffer.OutputDescription);

            Pipeline = disposeFactory.CreateGraphicsPipeline(ref pd);

            //Reset the buffer so all styles will update in OnRenderBegin
            Array.Fill(_cachedLightStyles, LightStyles.InvalidLightValue);

            //Float arrays are handled by padding each element up to the size of a vec4,
            //so we have to account for the padding in creating and initializing the buffer
            var numLightStyleElements = BSPConstants.MaxLightStyles * LightStylesElementMultiplier;

            LightStylesBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)(numLightStyleElements * Marshal.SizeOf<float>()), BufferUsage.UniformBuffer));

            //Initialize the buffer to all zeroes
            var lightStylesValues = new float[numLightStyleElements];
            Array.Fill(lightStylesValues, 0.0f);
            gd.UpdateBuffer(LightStylesBuffer, 0, lightStylesValues);
        }

        public void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();

            LightStylesBuffer = null;
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

                        //Convert to normalized [0, 1] range
                        var inputValue = value / (float)byte.MaxValue;

                        //Index is multiplied here because of padding. See buffer creation code
                        gd.UpdateBuffer(LightStylesBuffer, (uint)(i * Marshal.SizeOf<float>() * LightStylesElementMultiplier), ref inputValue);
                    }
                }
            }
        }
    }
}
