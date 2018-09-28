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

using SharpLife.Game.Client.Renderer.Shared.Utility;
using SharpLife.Game.Shared.Models;
using SharpLife.Models.MDL;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Models.MDL.Rendering;
using SharpLife.Renderer.Utility;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Models.MDL
{
    public sealed class StudioModelRenderer : IResourceContainer
    {
        private const int CullBack = 0;
        private const int CullFront = 1;
        private const int CullModeCount = 2;

        private const int MaskDisabled = 0;
        private const int MaskEnabled = 1;
        private const int MaskModeCount = 2;

        private const int AdditiveDisabled = 0;
        private const int AdditiveEnabled = 1;
        private const int AdditiveModeCount = 2;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public StudioModelBoneCalculator BoneCalculator { get; } = new StudioModelBoneCalculator();

        public DeviceBuffer BonesBuffer { get; private set; }

        public DeviceBuffer RenderArgumentsBuffer { get; private set; }

        public ResourceLayout SharedLayout { get; private set; }

        public ResourceLayout TextureLayout { get; private set; }

        private readonly RenderModePipelines[,,] _pipelines = new RenderModePipelines[CullModeCount, MaskModeCount, AdditiveModeCount];

        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            var factory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            BonesBuffer = factory.CreateBuffer(new BufferDescription((uint)(Marshal.SizeOf<Matrix4x4>() * MDLConstants.MaxBones), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            RenderArgumentsBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<StudioRenderArguments>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            SharedLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Bones", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("RenderArguments", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            TextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
               new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            var vertexLayouts = new[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("BoneIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt1))
            };

            for (var cullMode = CullBack; cullMode < CullModeCount; ++cullMode)
            {
                for (var maskMode = MaskDisabled; maskMode < MaskModeCount; ++maskMode)
                {
                    for (var additiveMode = AdditiveDisabled; additiveMode < AdditiveModeCount; ++additiveMode)
                    {
                        _pipelines[cullMode, maskMode, additiveMode] = CreatePipelines(
                            gd, sc, vertexLayouts, SharedLayout, TextureLayout, factory,
                            cullMode == CullBack,
                            maskMode == MaskEnabled,
                            additiveMode == AdditiveEnabled);
                    }
                }
            }
        }

        private static RenderModePipelines CreatePipelines(
            GraphicsDevice gd,
            SceneContext sc,
            VertexLayoutDescription[] vertexLayouts,
            ResourceLayout sharedLayout,
            ResourceLayout textureLayout,
            ResourceFactory factory,
            bool cullBack,
            bool masked,
            bool additive)
        {
            (var vs, var fs) = sc.MapResourceCache.GetShaders(gd, gd.ResourceFactory, Path.Combine("studio", "StudioGeneric"));

            var rasterizerState = new RasterizerStateDescription(cullBack ? FaceCullMode.Back : FaceCullMode.Front, PolygonFillMode.Solid, FrontFace.Clockwise, true, true);
            const PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList;
            var shaderSets = new ShaderSetDescription(vertexLayouts, new[] { vs, fs });
            var resourceLayouts = new ResourceLayout[] { sharedLayout, textureLayout };
            var outputDescription = sc.MainSceneFramebuffer.OutputDescription;

            var pipelines = new Pipeline[(int)RenderMode.Last + 1];

            BlendStateDescription normalBlendState;
            DepthStencilStateDescription normalDepthStencilState;

            if (additive)
            {
                normalBlendState = BlendStates.SingleAdditiveOneOneBlend;
                normalDepthStencilState = gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqualRead : DepthStencilStateDescription.DepthOnlyLessEqualRead;
            }
            else
            {
                normalBlendState = BlendStateDescription.SingleDisabled;
                normalDepthStencilState = gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual;
            }

            var pd = new GraphicsPipelineDescription(
                normalBlendState,
                normalDepthStencilState,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.Normal] = factory.CreateGraphicsPipeline(ref pd);

            pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.TransTexture] = factory.CreateGraphicsPipeline(ref pd);

            //These all use the same settings as texture
            pipelines[(int)RenderMode.TransColor] = pipelines[(int)RenderMode.TransTexture];
            pipelines[(int)RenderMode.Glow] = pipelines[(int)RenderMode.TransTexture];
            pipelines[(int)RenderMode.TransAlpha] = pipelines[(int)RenderMode.TransTexture];

            DepthStencilStateDescription additiveDepthState;

            if (masked)
            {
                additiveDepthState = gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual;
            }
            else
            {
                additiveDepthState = gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqualRead : DepthStencilStateDescription.DepthOnlyLessEqualRead;
            }

            pd = new GraphicsPipelineDescription(
                BlendStates.SingleAdditiveOneOneBlend,
                additiveDepthState,
                rasterizerState,
                primitiveTopology,
                shaderSets,
                resourceLayouts,
                outputDescription);

            pipelines[(int)RenderMode.TransAdd] = factory.CreateGraphicsPipeline(ref pd);

            return new RenderModePipelines(pipelines);
        }

        private RenderModePipelines GetPipelines(bool cullBack, bool masked, bool additive)
        {
            var cullMode = cullBack ? CullBack : CullFront;

            var maskMode = masked ? MaskEnabled : MaskDisabled;

            var additiveMode = additive ? AdditiveEnabled : AdditiveDisabled;

            return _pipelines[cullMode, maskMode, additiveMode];
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

        private Vector4 GetStudioColor(ref SharedModelRenderData renderData)
        {
            //TODO: seems that while the engine does set the color based on render mode, it will override it depending on texture flags
            //TODO: so this is really inconsistent
            //TODO: this is used only if we're doing glow shell
            switch (renderData.RenderMode)
            {
                //case RenderMode.Normal:
                //case RenderMode.TransColor: return Vector4.One;
                //case RenderMode.TransAdd: return new Vector4(renderData.RenderAmount / 255.0f, renderData.RenderAmount / 255.0f, renderData.RenderAmount / 255.0f, 1.0f);
                default: return new Vector4(1.0f, 1.0f, 1.0f, renderData.RenderAmount / 255.0f);
            }
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, StudioModelResourceContainer modelResource, ref StudioModelRenderData renderData)
        {
            if (renderData.Skin >= renderData.Model.StudioFile.Skins.Count)
            {
                renderData.Skin = 0;
            }

            //TODO: implement
            var wai = new WorldAndInverse(renderData.Shared.Origin, renderData.Shared.Angles, renderData.Shared.Scale);

            sc.UpdateWorldAndInverseBuffer(cl, ref wai);

            var bones = BoneCalculator.SetUpBones(modelResource.StudioModel.StudioFile,
                renderData.CurrentTime,
                renderData.Sequence,
                renderData.LastTime,
                renderData.Frame,
                renderData.FrameRate,
                renderData.BoneData);

            cl.UpdateBuffer(BonesBuffer, 0, bones);

            var renderArguments = new StudioRenderArguments
            {
                RenderColor = GetStudioColor(ref renderData.Shared),
            };

            cl.UpdateBuffer(RenderArgumentsBuffer, 0, ref renderArguments);

            //Determine which pipelines to use
            var isFrontCull = (renderData.Shared.Scale.X * renderData.Shared.Scale.Y * renderData.Shared.Scale.Z) >= 0;

            cl.SetVertexBuffer(0, modelResource.VertexBuffer);
            cl.SetIndexBuffer(modelResource.IndexBuffer, IndexFormat.UInt32);

            IReadOnlyList<int> skinRef = modelResource.StudioModel.StudioFile.Skins[(int)renderData.Skin];

            for (var bodyPartIndex = 0; bodyPartIndex < modelResource.BodyParts.Length; ++bodyPartIndex)
            {
                var bodyPart = modelResource.BodyParts[bodyPartIndex];

                var subModelIndex = StudioModelUtils.GetBodyGroupValue(renderData.Model.StudioFile, renderData.Body, (uint)bodyPartIndex);

                var subModel = bodyPart.SubModels[subModelIndex];

                foreach (var mesh in subModel.Meshes)
                {
                    var textureIndex = skinRef[mesh.Mesh.Skin];

                    var texture = renderData.Model.StudioFile.Textures[textureIndex];

                    var pipelines = GetPipelines(
                        isFrontCull,
                        (texture.Flags & MDLTextureFlags.Masked) != 0,
                        (texture.Flags & MDLTextureFlags.Additive) != 0);

                    var pipeline = pipelines[renderData.Shared.RenderMode];

                    cl.SetPipeline(pipeline);
                    cl.SetGraphicsResourceSet(0, modelResource.SharedResourceSet);
                    //TODO: consider possibility that there are no skins at all?
                    cl.SetGraphicsResourceSet(1, modelResource.Textures[textureIndex]);

                    cl.DrawIndexed(mesh.IndicesCount, 1, mesh.StartIndex, 0, 0);
                }
            }
        }
    }
}
