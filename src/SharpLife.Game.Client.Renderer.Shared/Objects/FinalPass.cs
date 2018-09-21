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

using SharpLife.Renderer;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Objects
{
    /// <summary>
    /// Takes the rendered scene and draws it into the swap chain buffer
    /// The scene is flipped vertically in the shader to ensure it's right side up
    /// </summary>
    public class FinalPass : ResourceContainer, IRenderable
    {
        private DisposeCollector _disposeCollector;
        private Pipeline _pipeline;
        private DeviceBuffer _ib;
        private DeviceBuffer _vb;

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Global) == 0)
            {
                return;
            }

            DisposeCollectorResourceFactory factory = new DisposeCollectorResourceFactory(gd.ResourceFactory);
            _disposeCollector = factory.DisposeCollector;

            ResourceLayout resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            var shaderName = gd.IsUvOriginTopLeft ? "FinalPass_inverted" : "FinalPass";

            (var vs, var fs) = sc.GlobalResourceCache.GetShaders(gd, gd.ResourceFactory, shaderName);

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                new BlendStateDescription(
                    RgbaFloat.Black,
                    BlendAttachmentDescription.OverrideBlend),
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                    },
                    new[] { vs, fs, }),
                new ResourceLayout[] { resourceLayout },
                gd.SwapchainFramebuffer.OutputDescription);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            float[] verts = Util.GetFullScreenQuadVerts(gd);

            _vb = factory.CreateBuffer(new BufferDescription(verts.SizeInBytes() * sizeof(float), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(_vb, 0, verts);

            _ib = factory.CreateBuffer(
                new BufferDescription((uint)s_quadIndices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(_ib, 0, s_quadIndices);
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Global) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey();
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, sc.MainSceneViewResourceSet);
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.DrawIndexed(6, 1, 0, 0, 0);
        }

        public RenderPasses RenderPasses => RenderPasses.SwapchainOutput;

        private static readonly ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    }
}
