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

using SharpLife.CommandSystem;
using SharpLife.FileSystem;
using SharpLife.Game.Client.Renderer.Shared.Models;
using SharpLife.Models.BSP;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace SharpLife.Game.Client.Renderer.Shared
{
    public class SceneContext
    {
        //Must be sizeof(vec4) / sizeof(float) so it matches the buffer padding
        public static readonly int LightStylesElementMultiplier = Marshal.SizeOf<Vector4>() / Marshal.SizeOf<float>();

        public ResourceCache GlobalResourceCache { get; }
        public ResourceCache MapResourceCache { get; }

        public TextureLoader TextureLoader { get; }

        public ModelRenderer ModelRenderer { get; }

        public DeviceBuffer ProjectionMatrixBuffer { get; private set; }
        public DeviceBuffer ViewMatrixBuffer { get; private set; }
        public DeviceBuffer CameraInfoBuffer { get; private set; }

        /// <summary>
        /// Since each render call updates this anyway, use a single shared world and inverse buffer for every object
        /// </summary>
        public DeviceBuffer WorldAndInverseBuffer { get; private set; }

        /// <summary>
        /// Contains lighting info for gamma correction and brightness adjustment
        /// </summary>
        public DeviceBuffer LightingInfoBuffer { get; private set; }

        public DeviceBuffer LightStylesBuffer { get; private set; }

        // MainSceneView resource set uses this.
        public ResourceLayout TextureSamplerResourceLayout { get; private set; }

        public Sampler MainSampler { get; private set; }

        public Texture MainSceneColorTexture { get; private set; }
        public Texture MainSceneDepthTexture { get; private set; }
        public Framebuffer MainSceneFramebuffer { get; private set; }
        public Texture MainSceneResolvedColorTexture { get; private set; }
        public TextureView MainSceneResolvedColorView { get; private set; }
        public ResourceSet MainSceneViewResourceSet { get; private set; }

        public Camera Camera { get; set; }

        public IViewState ViewState { get; set; }

        public Scene Scene { get; set; }

        public TextureSampleCount MainSceneSampleCount { get; internal set; }

        public SceneContext(IFileSystem fileSystem, ICommandContext commandContext, ModelRenderer modelRenderer, string shadersDirectory)
        {
            GlobalResourceCache = new ResourceCache(fileSystem, shadersDirectory);
            MapResourceCache = new ResourceCache(fileSystem, shadersDirectory);
            TextureLoader = new TextureLoader(commandContext);
            ModelRenderer = modelRenderer ?? throw new ArgumentNullException(nameof(modelRenderer));
        }

        public virtual void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            var factory = gd.ResourceFactory;
            ProjectionMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            ViewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            CameraInfoBuffer = factory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<CameraInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            WorldAndInverseBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<WorldAndInverse>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightingInfoBuffer = factory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<LightingInfo>(), BufferUsage.UniformBuffer));

            //Float arrays are handled by padding each element up to the size of a vec4,
            //so we have to account for the padding in creating and initializing the buffer
            var numLightStyleElements = BSPConstants.MaxLightStyles * LightStylesElementMultiplier;

            LightStylesBuffer = factory.CreateBuffer(new BufferDescription((uint)(numLightStyleElements * Marshal.SizeOf<int>()), BufferUsage.UniformBuffer));

            //Initialize the buffer to all zeroes
            var lightStylesValues = new float[numLightStyleElements];
            Array.Fill(lightStylesValues, 0.0f);
            gd.UpdateBuffer(LightStylesBuffer, 0, lightStylesValues);

            //TODO: pull filter settings and anisotropy from config
            var mainSamplerDescription = new SamplerDescription
            {
                AddressModeU = SamplerAddressMode.Wrap,
                AddressModeV = SamplerAddressMode.Wrap,
                AddressModeW = SamplerAddressMode.Wrap,
                Filter = SamplerFilter.MinLinear_MagLinear_MipLinear,
                LodBias = 0,
                MinimumLod = 0,
                MaximumLod = uint.MaxValue,
                MaximumAnisotropy = 4,
            };

            MainSampler = factory.CreateSampler(ref mainSamplerDescription);

            if (Camera != null)
            {
                UpdateCameraBuffers(cl);
            }

            TextureSamplerResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            RecreateWindowSizedResources(gd, cl);
        }

        public virtual void DestroyDeviceObjects()
        {
            ProjectionMatrixBuffer.Dispose();
            ViewMatrixBuffer.Dispose();
            CameraInfoBuffer.Dispose();
            WorldAndInverseBuffer.Dispose();
            LightingInfoBuffer.Dispose();
            LightingInfoBuffer = null;
            LightStylesBuffer.Dispose();
            MainSampler.Dispose();
            MainSceneColorTexture.Dispose();
            MainSceneResolvedColorTexture.Dispose();
            MainSceneResolvedColorView.Dispose();
            MainSceneDepthTexture.Dispose();
            MainSceneFramebuffer.Dispose();
            MainSceneViewResourceSet.Dispose();
            TextureSamplerResourceLayout.Dispose();
        }

        public void SetCurrentScene(Scene scene)
        {
            Camera = scene.Camera;
            ViewState = scene;
            Scene = scene;
        }

        public unsafe void UpdateCameraBuffers(CommandList cl)
        {
            cl.UpdateBuffer(ProjectionMatrixBuffer, 0, Camera.ProjectionMatrix);
            cl.UpdateBuffer(ViewMatrixBuffer, 0, Camera.ViewMatrix);
            cl.UpdateBuffer(CameraInfoBuffer, 0, Camera.GetCameraInfo());
        }

        public void UpdateWorldAndInverseBuffer(CommandList cl, ref WorldAndInverse wai)
        {
            cl.UpdateBuffer(WorldAndInverseBuffer, 0, ref wai);
        }

        public void RecreateWindowSizedResources(GraphicsDevice gd, CommandList cl)
        {
            MainSceneColorTexture?.Dispose();
            MainSceneDepthTexture?.Dispose();
            MainSceneResolvedColorTexture?.Dispose();
            MainSceneResolvedColorView?.Dispose();
            MainSceneViewResourceSet?.Dispose();
            MainSceneFramebuffer?.Dispose();

            ResourceFactory factory = gd.ResourceFactory;

            TextureSampleCount mainSceneSampleCountCapped = (TextureSampleCount)Math.Min(
                (int)gd.GetSampleCountLimit(PixelFormat.R8_G8_B8_A8_UNorm, false),
                (int)MainSceneSampleCount);

            TextureDescription mainColorDesc = TextureDescription.Texture2D(
                gd.SwapchainFramebuffer.Width,
                gd.SwapchainFramebuffer.Height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                mainSceneSampleCountCapped);

            MainSceneColorTexture = factory.CreateTexture(ref mainColorDesc);
            if (mainSceneSampleCountCapped != TextureSampleCount.Count1)
            {
                mainColorDesc.SampleCount = TextureSampleCount.Count1;
                MainSceneResolvedColorTexture = factory.CreateTexture(ref mainColorDesc);
            }
            else
            {
                MainSceneResolvedColorTexture = MainSceneColorTexture;
            }
            MainSceneResolvedColorView = factory.CreateTextureView(MainSceneResolvedColorTexture);
            MainSceneDepthTexture = factory.CreateTexture(TextureDescription.Texture2D(
                gd.SwapchainFramebuffer.Width,
                gd.SwapchainFramebuffer.Height,
                1,
                1,
                PixelFormat.R32_Float,
                TextureUsage.DepthStencil,
                mainSceneSampleCountCapped));
            MainSceneFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(MainSceneDepthTexture, MainSceneColorTexture));
            MainSceneViewResourceSet = factory.CreateResourceSet(new ResourceSetDescription(TextureSamplerResourceLayout, MainSceneResolvedColorView, gd.PointSampler));
        }

        public Face SurfaceAtPoint(in Vector3 start, in Vector3 end)
        {
            return BSPModelUtils.SurfaceAtPoint(Scene.WorldModel.SubModel, Scene.WorldModel.BSPFile.Nodes[0], start, end);
        }

        public Rgb24 LightFromTrace(in Vector3 start, in Vector3 end)
        {
            if (Scene.WorldModel.BSPFile.Lighting != null)
            {
                var color = BSPModelUtils.RecursiveLightPoint(
                    Scene.WorldModel.SubModel,
                    Scene.WorldModel.BSPFile.Lighting,
                    Scene.LightStyles,
                    Scene.WorldModel.BSPFile.Nodes[0],
                    start, end);

                return color;

                //TODO: this is done in the original engine, but nothing ever sets ambientlight
                /*
                return new Rgba32
                (
                    Math.Min(byte.MaxValue, color.R + r_refdef.ambientlight.r),
                    Math.Min(byte.MaxValue, color.G + r_refdef.ambientlight.g),
                    Math.Min(byte.MaxValue, color.B + r_refdef.ambientlight.b),
                    color.A
                );
                */
            }
            else
            {
                return new Rgb24(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            }
        }
    }
}
