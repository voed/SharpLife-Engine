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
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Game.Client.Renderer.Shared.Utility;
using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Models;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.MDL;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Models.MDL.Rendering;
using SharpLife.Renderer.Utility;
using System;
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

        private const int MaxLocalLights = 3;

        private struct LocalLight
        {
            public DynamicLight Light;
            public float RadiusSquared;
        }

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private readonly IVariable _direct;

        private readonly LocalLight[] _localLights = new LocalLight[MaxLocalLights];

        private int _numLocalLights;

        public StudioModelBoneCalculator BoneCalculator { get; } = new StudioModelBoneCalculator();

        public DeviceBuffer BonesBuffer { get; private set; }

        public DeviceBuffer RenderArgumentsBuffer { get; private set; }

        public DeviceBuffer TextureDataBuffer { get; private set; }

        private ResourceLayout _sharedLayout;

        public ResourceSet SharedResourceSet { get; private set; }

        public ResourceLayout TextureLayout { get; private set; }

        private readonly RenderModePipelines[,,] _pipelines = new RenderModePipelines[CullModeCount, MaskModeCount, AdditiveModeCount];

        public StudioModelRenderer(ICommandContext commandContext)
        {
            _direct = commandContext.RegisterVariable(new VariableInfo("direct")
                .WithHelpInfo("Controls the shade light multiplier")
                .WithValue(0.9f)
                .WithMinMaxFilter(0.75f, 1.0f));
        }

        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            var factory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            BonesBuffer = factory.CreateBuffer(new BufferDescription((uint)(Marshal.SizeOf<Matrix4x4>() * MDLConstants.MaxBones), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            RenderArgumentsBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<StudioRenderArguments>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            TextureDataBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<StudioTextureData>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            _sharedLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Bones", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("RenderArguments", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("TextureData", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightingInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            TextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
               new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            var vertexLayouts = new[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3),
                    new VertexElementDescription("VertexBoneIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt1),
                    new VertexElementDescription("NormalBoneIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt1))
            };

            for (var cullMode = CullBack; cullMode < CullModeCount; ++cullMode)
            {
                for (var maskMode = MaskDisabled; maskMode < MaskModeCount; ++maskMode)
                {
                    for (var additiveMode = AdditiveDisabled; additiveMode < AdditiveModeCount; ++additiveMode)
                    {
                        _pipelines[cullMode, maskMode, additiveMode] = CreatePipelines(
                            gd, sc, vertexLayouts, _sharedLayout, TextureLayout, factory,
                            cullMode == CullBack,
                            maskMode == MaskEnabled,
                            additiveMode == AdditiveEnabled);
                    }
                }
            }

            SharedResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _sharedLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                sc.WorldAndInverseBuffer,
                BonesBuffer,
                RenderArgumentsBuffer,
                TextureDataBuffer,
                sc.MainSampler,
                sc.LightingInfoBuffer
                ));
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

        private AmbientLight StudioDynamicLight(ref StudioModelRenderData renderData, SceneContext sc)
        {
            if (sc.Scene.Fullbright.Integer == 1)
            {
                return new AmbientLight
                {
                    Ambient = AmbientLight.MaxShade,
                    Shade = 0,
                    Normal = new Vector3(0, 0, -1),
                    Color = Vector3.One
                };
            }

            var invertLightSource = (renderData.Shared.Effects & EffectsFlags.InvLight) != 0;

            var light = new Vector3(0, 0, invertLightSource ? 1 : -1);

            var uporigin = new Vector3(
                renderData.Shared.Origin.X,
                renderData.Shared.Origin.Y,
                renderData.Shared.Origin.Z - (invertLightSource ? 8 : -8));

            var color = new Vector3();

            var useSkyColor = false;

            var skyColor = sc.Scene.SkyColor;

            if ((skyColor.X + skyColor.Y + skyColor.Z) != 0)
            {
                var end = renderData.Shared.Origin - (sc.Scene.SkyNormal * 8192.0f);

                var pSurface = sc.SurfaceAtPoint(uporigin, end);

                if ((renderData.Model.StudioFile.Flags & MDLFlags.ForceSkyLight) != 0 || (pSurface != null && (pSurface.Flags & FaceFlags.Sky) != 0))
                {
                    color = skyColor;

                    light = sc.Scene.SkyNormal;

                    useSkyColor = true;
                }
            }

            if (!useSkyColor)
            {
                var upend = uporigin + (light * 2048.0f);

                var lightColor = sc.LightFromTrace(uporigin, upend);

                color.X = lightColor.R;
                color.Y = lightColor.G;
                color.Z = lightColor.B;

                uporigin.X -= 16.0f;
                uporigin.Y -= 16.0f;

                upend.X -= 16.0f;
                upend.Y -= 16.0f;

                var color1 = sc.LightFromTrace(uporigin, upend);
                var color1Normalized = (color1.R + color1.G + color1.B) / 768.0f;

                uporigin.X += 32.0f;
                upend.X += 32.0f;

                var color2 = sc.LightFromTrace(uporigin, upend);
                var color2Normalized = (color2.R + color2.G + color2.B) / 768.0f;

                uporigin.Y += 32.0f;
                upend.Y += 32.0f;

                var color3 = sc.LightFromTrace(uporigin, upend);
                var color3Normalized = (color3.R + color3.G + color3.B) / 768.0f;

                uporigin.X -= 32.0f;
                upend.X -= 32.0f;

                var color4 = sc.LightFromTrace(uporigin, upend);
                var color4Normalized = (color4.R + color4.G + color4.B) / 768.0f;

                light.X = color1Normalized - color2Normalized - color3Normalized + color4Normalized;
                light.Y = color1Normalized + color2Normalized - color3Normalized - color4Normalized;

                light = Vector3.Normalize(light);
            }

            if (renderData.Shared.RenderFX == RenderFX.LightMultiplier)
            {
                if (renderData.RenderFXLightMultiplier != 10)
                {
                    var lightMultiplier = (int)(renderData.RenderFXLightMultiplier / 10.0);

                    color.X *= lightMultiplier;
                    color.Y *= lightMultiplier;
                    color.Z *= lightMultiplier;
                }
            }

            //TODO: figure out how to store this off
            //currententity->cvFloorColor = color;

            var red = color.X;
            var green = color.Y;
            var blue = color.Z;

            var brightestComponent = Math.Max(color.X, Math.Max(color.Y, color.Z));

            var floor = brightestComponent != 0 ? brightestComponent : 1.0f;

            light *= floor;

            for (var i = 0; i < Scene.MaxDLights; ++i)
            {
                ref var dlight = ref sc.Scene.DynamicLights[i];

                if (dlight.Die >= renderData.CurrentTime)
                {
                    var dist = renderData.Shared.Origin - dlight.Origin;

                    var length = dist.Length();
                    var falloff = dlight.Radius - length;

                    if (falloff > 0.0)
                    {
                        var inverseFalloff = falloff;

                        if (length > 1.0)
                        {
                            inverseFalloff = falloff / length;
                        }

                        floor += falloff;

                        dist *= inverseFalloff;

                        light += dist;

                        red += dlight.Color.R * falloff;
                        green += dlight.Color.G * falloff;
                        blue += dlight.Color.B * falloff;
                    }
                }
            }

            var shadeLight = 0.6f;

            if ((renderData.Model.StudioFile.Flags & MDLFlags.NoShadeLight) == 0)
            {
                shadeLight = _direct.Float;
            }

            light *= shadeLight;

            var ambientLight = new AmbientLight
            {
                Shade = (int)light.Length()
            };

            var brightestComponent2 = Math.Max(red, Math.Max(green, blue));

            ambientLight.Ambient = (int)(floor - ambientLight.Shade);

            if (brightestComponent2 == 0)
            {
                ambientLight.Color = Vector3.One;
            }
            else
            {
                //Rescale the components so the strongest component is value 1 (brightest)
                var inverseBrightest = 1.0f / brightestComponent2;

                ambientLight.Color = new Vector3(
                    red * inverseBrightest,
                    green * inverseBrightest,
                    blue * inverseBrightest
                    );
            }

            ambientLight.Ambient = Math.Min(AmbientLight.MaxAmbient, ambientLight.Ambient);

            if (ambientLight.Ambient + ambientLight.Shade > byte.MaxValue)
            {
                ambientLight.Shade = byte.MaxValue - ambientLight.Ambient;
            }

            light = Vector3.Normalize(light);

            ambientLight.Normal = light;

            return ambientLight;
        }

        private unsafe void StudioEntityLight(in AmbientLight ambientLight, ref StudioModelRenderData renderData, SceneContext sc)
        {
            _numLocalLights = 0;

            float* lstrength = stackalloc float[MaxLocalLights];

            var closest = 1000000.0;

            for (int i = 0; i < Scene.MaxELights; ++i)
            {
                ref var light = ref sc.Scene.EntityLights[i];

                if (light.Die > renderData.CurrentTime && light.Radius > 0.0)
                {
                    if ((light.Key & 0xFFF) == renderData.Shared.Index)
                    {
                        var attachmentIndex = (light.Key >> 12) & 0xF;

                        if (attachmentIndex != 0)
                        {
                            //TODO: set up attachments
                            light.Origin = renderData.Shared.Origin;// currententity->attachment[attachmentIndex - 1];
                        }
                        else
                        {
                            light.Origin = renderData.Shared.Origin;
                        }
                    }

                    var distanceSquared = (renderData.Shared.Origin - light.Origin).LengthSquared();
                    var radiusSquared = light.Radius * light.Radius;

                    var falloff = distanceSquared <= radiusSquared ? 1.0 : (radiusSquared / distanceSquared);

                    if (falloff > 0.004)
                    {
                        var lightIndex = _numLocalLights;

                        if (_numLocalLights >= MaxLocalLights)
                        {
                            lightIndex = -1;

                            for (int localIndex = 0; localIndex < _numLocalLights; ++localIndex)
                            {
                                if (closest > lstrength[localIndex] && falloff > lstrength[localIndex])
                                {
                                    closest = lstrength[localIndex];
                                    lightIndex = localIndex;
                                }
                            }
                        }

                        if (lightIndex != -1)
                        {
                            _localLights[lightIndex] = new LocalLight
                            {
                                Light = light,
                                RadiusSquared = radiusSquared,
                            };

                            lstrength[lightIndex] = (float)falloff;

                            if (lightIndex >= _numLocalLights)
                            {
                                _numLocalLights = lightIndex + 1;
                            }
                        }
                    }
                }
            }
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

            //Set up lighting
            var ambientLight = StudioDynamicLight(ref renderData, sc);

            StudioEntityLight(ambientLight, ref renderData, sc);

            var renderArguments = new StudioRenderArguments
            {
                RenderColor = GetStudioColor(ref renderData.Shared),
                GlobalLight = new StudioRenderArguments.AmbientLight
                {
                    Ambient = ambientLight.Ambient,
                    Shade = ambientLight.Shade,
                    Color = ambientLight.Color,
                    Normal = Vector3.Transform(ambientLight.Normal, RenderUtils.Identity)
                }
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

                    var textureData = new StudioTextureData
                    {
                        FlatShade = (texture.Flags & MDLTextureFlags.FlatShade) != 0 ? 1 : 0,
                    };

                    cl.UpdateBuffer(TextureDataBuffer, 0, ref textureData);

                    var pipelines = GetPipelines(
                        isFrontCull,
                        (texture.Flags & MDLTextureFlags.Masked) != 0,
                        (texture.Flags & MDLTextureFlags.Additive) != 0);

                    var pipeline = pipelines[renderData.Shared.RenderMode];

                    cl.SetPipeline(pipeline);
                    cl.SetGraphicsResourceSet(0, SharedResourceSet);
                    //TODO: consider possibility that there are no skins at all?
                    cl.SetGraphicsResourceSet(1, modelResource.Textures[textureIndex]);

                    cl.DrawIndexed(mesh.IndicesCount, 1, mesh.StartIndex, 0, 0);
                }
            }
        }
    }
}
