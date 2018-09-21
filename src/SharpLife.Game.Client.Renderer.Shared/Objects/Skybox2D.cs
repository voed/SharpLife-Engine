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

using SharpLife.FileSystem;
using SharpLife.Renderer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Game.Client.Renderer.Shared.Objects
{
    /// <summary>
    /// Draws the 2D skybox at the outer edges of the world
    /// </summary>
    public class Skybox2D : ResourceContainer, IRenderable
    {
        private struct CubemapPosition
        {
            public Vector3 vertex;
            public Vector3 textureCoords;

            public CubemapPosition(Vector3 vertex, Vector3 textureCoords)
            {
                this.vertex = vertex;
                this.textureCoords = textureCoords;
            }
        }

        private readonly Image<Rgba32> _front;
        private readonly Image<Rgba32> _back;
        private readonly Image<Rgba32> _left;
        private readonly Image<Rgba32> _right;
        private readonly Image<Rgba32> _top;
        private readonly Image<Rgba32> _bottom;

        // Context objects
        private DeviceBuffer _vb;
        private DeviceBuffer _ib;
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _layout;
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        public Skybox2D(
            Image<Rgba32> front, Image<Rgba32> back, Image<Rgba32> left,
            Image<Rgba32> right, Image<Rgba32> top, Image<Rgba32> bottom)
        {
            _front = front;
            _back = back;
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
        }

        public unsafe override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            ResourceFactory factory = gd.ResourceFactory;

            _vb = factory.CreateBuffer(new BufferDescription(s_vertices.SizeInBytes(), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(_vb, 0, s_vertices);

            _ib = factory.CreateBuffer(new BufferDescription(s_indices.SizeInBytes(), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(_ib, 0, s_indices);

            Texture textureCube;
            TextureView textureView;
            fixed (Rgba32* frontPin = &MemoryMarshal.GetReference(_front.GetPixelSpan()))
            {
                fixed (Rgba32* backPin = &MemoryMarshal.GetReference(_back.GetPixelSpan()))
                {
                    fixed (Rgba32* leftPin = &MemoryMarshal.GetReference(_left.GetPixelSpan()))
                    {
                        fixed (Rgba32* rightPin = &MemoryMarshal.GetReference(_right.GetPixelSpan()))
                        {
                            fixed (Rgba32* topPin = &MemoryMarshal.GetReference(_top.GetPixelSpan()))
                            {
                                fixed (Rgba32* bottomPin = &MemoryMarshal.GetReference(_bottom.GetPixelSpan()))
                                {
                                    uint width = (uint)_front.Width;
                                    uint height = (uint)_front.Height;
                                    textureCube = factory.CreateTexture(TextureDescription.Texture2D(
                                        width,
                                        height,
                                        1,
                                        1,
                                        PixelFormat.R8_G8_B8_A8_UNorm,
                                        TextureUsage.Sampled | TextureUsage.Cubemap));

                                    uint faceSize = (uint)(_front.Width * _front.Height * Unsafe.SizeOf<Rgba32>());
                                    gd.UpdateTexture(textureCube, (IntPtr)leftPin, faceSize, 0, 0, 0, width, height, 1, 0, 0);
                                    gd.UpdateTexture(textureCube, (IntPtr)rightPin, faceSize, 0, 0, 0, width, height, 1, 0, 1);
                                    gd.UpdateTexture(textureCube, (IntPtr)backPin, faceSize, 0, 0, 0, width, height, 1, 0, 2);
                                    gd.UpdateTexture(textureCube, (IntPtr)frontPin, faceSize, 0, 0, 0, width, height, 1, 0, 3);
                                    gd.UpdateTexture(textureCube, (IntPtr)topPin, faceSize, 0, 0, 0, width, height, 1, 0, 4);
                                    gd.UpdateTexture(textureCube, (IntPtr)bottomPin, faceSize, 0, 0, 0, width, height, 1, 0, 5);

                                    textureView = factory.CreateTextureView(new TextureViewDescription(textureCube));
                                }
                            }
                        }
                    }
                }
            }

            //Because GoldSource's coordinate system isn't the default OpenGL one, we have to pass texture coordinates seperately
            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3))
            };

            (Shader vs, Shader fs) = sc.MapResourceCache.GetShaders(gd, gd.ResourceFactory, "Skybox2D");

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("CubeTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("CubeSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { _layout },
                sc.MainSceneFramebuffer.OutputDescription);

            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                textureView,
                gd.PointSampler));

            _disposeCollector.Add(_vb, _ib, textureCube, textureView, _layout, _pipeline, _resourceSet, vs, fs);
        }

        public static Skybox2D LoadDefaultSkybox(IFileSystem fileSystem, string envDirectory, string skyboxName)
        {
            return new Skybox2D(
                Image.Load(fileSystem.OpenRead(Path.Combine(envDirectory, skyboxName + "ft.bmp"))),
                Image.Load(fileSystem.OpenRead(Path.Combine(envDirectory, skyboxName + "bk.bmp"))),
                Image.Load(fileSystem.OpenRead(Path.Combine(envDirectory, skyboxName + "lf.bmp"))),
                Image.Load(fileSystem.OpenRead(Path.Combine(envDirectory, skyboxName + "rt.bmp"))),
                Image.Load(fileSystem.OpenRead(Path.Combine(envDirectory, skyboxName + "up.bmp"))),
                Image.Load(fileSystem.OpenRead(Path.Combine(envDirectory, skyboxName + "dn.bmp"))));
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _resourceSet);
            float depth = gd.IsDepthRangeZeroToOne ? 0 : 1;
            cl.SetViewport(0, new Viewport(0, 0, sc.MainSceneColorTexture.Width, sc.MainSceneColorTexture.Height, depth, depth));
            cl.DrawIndexed((uint)s_indices.Length, 1, 0, 0, 0);
        }

        public RenderPasses RenderPasses => RenderPasses.Standard;

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        private static readonly CubemapPosition[] s_vertices = new CubemapPosition[]
        {
            // Left
            new CubemapPosition(new Vector3(-20.0f,20.0f,-20.0f), new Vector3(20.0f,20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,20.0f,-20.0f), new Vector3(-20.0f,20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,20.0f,20.0f), new Vector3(-20.0f,20.0f,-20.0f)),
            new CubemapPosition(new Vector3(-20.0f,20.0f,20.0f), new Vector3(20.0f,20.0f,-20.0f)),
            // Right
            new CubemapPosition(new Vector3(-20.0f,-20.0f,20.0f), new Vector3(-20.0f,-20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,-20.0f,20.0f), new Vector3(20.0f,-20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,-20.0f,-20.0f), new Vector3(20.0f,-20.0f,-20.0f)),
            new CubemapPosition(new Vector3(-20.0f,-20.0f,-20.0f), new Vector3(-20.0f,-20.0f,-20.0f)),
            // Back
            new CubemapPosition(new Vector3(-20.0f,20.0f,-20.0f), new Vector3(-20.0f,-20.0f,-20.0f)),
            new CubemapPosition(new Vector3(-20.0f,20.0f,20.0f), new Vector3(-20.0f,20.0f,-20.0f)),
            new CubemapPosition(new Vector3(-20.0f,-20.0f,20.0f), new Vector3(-20.0f,20.0f,20.0f)),
            new CubemapPosition(new Vector3(-20.0f,-20.0f,-20.0f), new Vector3(-20.0f,-20.0f,20.0f)),
            // Front
            new CubemapPosition(new Vector3(20.0f,20.0f,20.0f), new Vector3(20.0f,20.0f,-20.0f)),
            new CubemapPosition(new Vector3(20.0f,20.0f,-20.0f), new Vector3(20.0f,-20.0f,-20.0f)),
            new CubemapPosition(new Vector3(20.0f,-20.0f,-20.0f), new Vector3(20.0f,-20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,-20.0f,20.0f), new Vector3(20.0f,20.0f,20.0f)),
            // Top
            new CubemapPosition(new Vector3(-20.0f,20.0f,20.0f), new Vector3(-20.0f,-20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,20.0f,20.0f), new Vector3(-20.0f,20.0f,20.0f)),
            new CubemapPosition(new Vector3(20.0f,-20.0f,20.0f), new Vector3(20.0f,20.0f,20.0f)),
            new CubemapPosition(new Vector3(-20.0f,-20.0f,20.0f), new Vector3(20.0f,-20.0f,20.0f)),
            // Bottom
            new CubemapPosition(new Vector3(20.0f,20.0f,-20.0f), new Vector3(20.0f,-20.0f,-20.0f)),
            new CubemapPosition(new Vector3(-20.0f,20.0f,-20.0f), new Vector3(20.0f,20.0f,-20.0f)),
            new CubemapPosition(new Vector3(-20.0f,-20.0f,-20.0f), new Vector3(-20.0f,20.0f,-20.0f)),
            new CubemapPosition(new Vector3(20.0f,-20.0f,-20.0f), new Vector3(-20.0f,-20.0f,-20.0f)),
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
    }
}
