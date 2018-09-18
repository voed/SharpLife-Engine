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

using SharpLife.Models.MDL.FileFormat;
using SharpLife.Models.MDL.Loading;
using SharpLife.Renderer;
using SharpLife.Renderer.Models;
using SharpLife.Renderer.Utility;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Models.MDL.Rendering
{
    public sealed class StudioModelResourceFactory : IModelResourceFactory
    {
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public StudioModelBoneCalculator BoneCalculator { get; } = new StudioModelBoneCalculator();

        public DeviceBuffer WorldAndInverseBuffer { get; private set; }

        public DeviceBuffer BonesBuffer { get; private set; }

        public ResourceLayout SharedLayout { get; private set; }

        public ResourceLayout TextureLayout { get; private set; }

        public Pipeline Pipeline { get; private set; }

        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            WorldAndInverseBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<WorldAndInverse>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            BonesBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)(Marshal.SizeOf<Matrix4x4>() * MDLConstants.MaxBones), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            SharedLayout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Bones", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            TextureLayout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
               new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            var vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("BoneIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt1))
            };

            (var vs, var fs) = sc.MapResourceCache.GetShaders(gd, gd.ResourceFactory, Path.Combine("studio", "StudioGeneric"));

            //Create render mode pipelines
            var rasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true);
            const PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList;
            var shaderSets = new ShaderSetDescription(vertexLayouts, new[] { vs, fs });
            var resourceLayouts = new ResourceLayout[] { SharedLayout, TextureLayout };
            var outputDescription = sc.MainSceneFramebuffer.OutputDescription;

            var pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            Pipeline = disposeFactory.CreateGraphicsPipeline(ref pd);
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
            if (!(model is StudioModel studioModel))
            {
                throw new ArgumentException("Model must be a Studio model", nameof(model));
            }

            return new StudioModelResourceContainer(this, studioModel);
        }
    }
}
