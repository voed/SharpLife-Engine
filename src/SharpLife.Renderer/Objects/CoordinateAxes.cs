using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer.Objects
{
    /// <summary>
    /// Draws the coordinate axes
    /// </summary>
    public class CoordinateAxes : Renderable
    {
        private struct LineData
        {
            public Vector3 vertex;
            public Vector3 color;
        }

        private DeviceBuffer _vb;
        private DeviceBuffer _ib;
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _layout;
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _resourceSet);
            cl.DrawIndexed((uint)s_indices.Length, 1, 0, 0, 0);
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            var factory = gd.ResourceFactory;

            _vb = factory.CreateBuffer(new BufferDescription(s_vertices.SizeInBytes(), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(_vb, 0, s_vertices);

            _ib = factory.CreateBuffer(new BufferDescription(s_indices.SizeInBytes(), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(_ib, 0, s_indices);

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float3))
            };

            (Shader vs, Shader fs) = sc.ResourceCache.GetShaders(gd, gd.ResourceFactory, "CoordinateAxes");

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.LineList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { _layout },
                sc.MainSceneFramebuffer.OutputDescription);

            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer));

            _disposeCollector.Add(_vb, _ib, _layout, _pipeline, _resourceSet, vs, fs);
        }

        public override void DestroyDeviceObjects()
        {
            _disposeCollector.DisposeAll();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey();
        }

        private static readonly Vector3 XColor = new Vector3(1, 0, 0);
        private static readonly Vector3 YColor = new Vector3(0, 1, 0);
        private static readonly Vector3 ZColor = new Vector3(0, 0, 1);

        private static readonly LineData[] s_vertices = new LineData[]
        {
            new LineData{vertex = new Vector3(0, 0, 0), color = XColor },
            new LineData{vertex = new Vector3(100, 0, 0), color = XColor },
            new LineData{vertex = new Vector3(0, 0, 0), color = YColor },
            new LineData{vertex = new Vector3(0, 100, 0), color = YColor },
            new LineData{vertex = new Vector3(0, 0, 0), color = ZColor },
            new LineData{vertex = new Vector3(0, 0, 100), color = ZColor },
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0, 1,
            2, 3,
            4, 5
        };
    }
}
