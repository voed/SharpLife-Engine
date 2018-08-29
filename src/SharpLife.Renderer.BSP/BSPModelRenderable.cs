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

using SharpLife.Engine.Shared.Models.BSP;
using SharpLife.FileFormats.BSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer.BSP
{
    /// <summary>
    /// The BSP World model renderable
    /// </summary>
    public class BSPModelRenderable : Renderable
    {
        public struct WorldAndInverse
        {
            public Matrix4x4 World;
            public Matrix4x4 InverseWorld;
        }

        private class FaceBufferData : IDisposable
        {
            public DeviceBuffer VertexBuffer;

            public DeviceBuffer IndexBuffer;

            public uint IndicesCount;

            public ResourceSet Texture;

            public void Dispose()
            {
                VertexBuffer.Dispose();
                IndexBuffer.Dispose();
                Texture.Dispose();
            }
        }

        private readonly BSPModel _bspModel;

        private List<FaceBufferData> _faces;
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _layout;
        private ResourceLayout _textureLayout;

        private DeviceBuffer _worldAndInverseBuffer;

        public Transform Transform { get; } = new Transform();

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public BSPModelRenderable(BSPModel bspModel)
        {
            _bspModel = bspModel ?? throw new ArgumentNullException(nameof(bspModel));
        }

        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            WorldAndInverse wai;
            wai.World = Transform.GetTransformMatrix();
            wai.InverseWorld = VdUtilities.CalculateInverseTranspose(ref wai.World);
            gd.UpdateBuffer(_worldAndInverseBuffer, 0, ref wai);
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            cl.SetPipeline(_pipeline);

            foreach (var faces in _faces)
            {
                cl.SetVertexBuffer(0, faces.VertexBuffer);
                cl.SetIndexBuffer(faces.IndexBuffer, IndexFormat.UInt32);
                cl.SetPipeline(_pipeline);
                cl.SetGraphicsResourceSet(0, _resourceSet);
                cl.SetGraphicsResourceSet(1, faces.Texture);
                cl.DrawIndexed(faces.IndicesCount, 1, 0, 0, 0);
            }
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            ResourceFactory disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            _worldAndInverseBuffer = disposeFactory.CreateBuffer(new BufferDescription(64 * 2, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            //Build a list of buffers, each buffer containing all of the faces that have the same texture
            //This reduces the number of buffers we have to create from a set for each face to a set for each texture and all of the faces referencing it
            //TODO: further split by visleaf when vis data is available
            var sortedFaces = _bspModel.SubModel.Faces.GroupBy(face => face.TextureInfo.MipTexture.Name);

            _faces = new List<FaceBufferData>();

            _textureLayout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            foreach (var faces in sortedFaces)
            {
                if (faces.Key != "sky")
                {
                    //Don't use the dispose factory because some of the created resources are already managed elsewhere
                    var facesData = BuildFacesBuffer(faces.ToList(), sc.ResourceCache, gd, cl, sc);
                    _disposeCollector.Add(facesData);
                    _faces.Add(facesData);
                }
            }

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
            };

            (Shader vs, Shader fs) = sc.ResourceCache.GetShaders(gd, gd.ResourceFactory, "LightMappedGeneric");

            _layout = disposeFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { _layout, _textureLayout },
                sc.MainSceneFramebuffer.OutputDescription);

            _pipeline = disposeFactory.CreateGraphicsPipeline(ref pd);

            _resourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                _worldAndInverseBuffer));
        }

        public override void DestroyDeviceObjects()
        {
            _disposeCollector.DisposeAll();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey();
        }

        private FaceBufferData BuildFacesBuffer(IReadOnlyList<Face> faces, ResourceCache resourceCache, GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (faces.Count == 0)
            {
                throw new ArgumentException("Cannot create a face buffer when no faces are provided");
            }

            var mipTexture = faces[0].TextureInfo.MipTexture;

            var vertices = new List<BSPCoordinate>();
            var indices = new List<uint>();

            foreach (var face in faces)
            {
                var firstVertex = vertices.Count;

                //Create triangles out of the face
                foreach (var i in Enumerable.Range(vertices.Count + 1, face.Points.Count - 2))
                {
                    indices.Add((uint)firstVertex);
                    indices.Add((uint)i);
                    indices.Add((uint)i + 1);
                }

                var textureInfo = face.TextureInfo;

                foreach (var point in face.Points)
                {
                    var s = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                    s /= mipTexture.Width;

                    var t = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                    t /= mipTexture.Height;

                    vertices.Add(new BSPCoordinate
                    {
                        vertex = point,
                        texture = new Vector2(s, t)
                    });
                }
            }

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            var indicesCount = (uint)indicesArray.Length;

            var factory = gd.ResourceFactory;

            var vb = factory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            cl.UpdateBuffer(vb, 0, verticesArray);

            var ib = factory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            cl.UpdateBuffer(ib, 0, indicesArray);

            var texture = resourceCache.GetTexture2D(mipTexture.Name);

            //If not found, fallback to have a valid texture
            texture = texture ?? resourceCache.GetPinkTexture(gd, factory);

            var view = resourceCache.GetTextureView(factory, texture);

            return new FaceBufferData
            {
                VertexBuffer = vb,
                IndexBuffer = ib,
                IndicesCount = indicesCount,
                Texture = factory.CreateResourceSet(new ResourceSetDescription(
                    _textureLayout,
                    view,
                    sc.MainSampler))
            };
        }
    }
}
