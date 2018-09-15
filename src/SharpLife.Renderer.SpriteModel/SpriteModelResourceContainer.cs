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

using SharpLife.FileFormats.SPR;
using SharpLife.Models;
using SharpLife.Renderer.Models;
using SharpLife.Renderer.Utility;
using SharpLife.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Utilities;

namespace SharpLife.Renderer.SpriteModel
{
    public class SpriteModelResourceContainer : ModelResourceContainer
    {
        private readonly SpriteModelResourceFactory _factory;

        private readonly SharpLife.Models.SPR.SpriteModel _spriteModel;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private readonly List<DeviceBuffer> _frameBuffers = new List<DeviceBuffer>();

        private DeviceBuffer _ib;
        private DeviceBuffer _worldAndInverseBuffer;
        private DeviceBuffer _renderColorBuffer;
        private ResourceSet _resourceSet;

        public override IModel Model => _spriteModel;

        public SpriteModelResourceContainer(SpriteModelResourceFactory factory, SharpLife.Models.SPR.SpriteModel spriteModel)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _spriteModel = spriteModel ?? throw new ArgumentNullException(nameof(spriteModel));
        }

        private Vector3 CalculateSpriteColor(ref ModelRenderData renderData, int alpha)
        {
            var colorMultiplier = renderData.RenderMode == RenderMode.Glow || renderData.RenderMode == RenderMode.TransAdd ? alpha : 256;

            //TODO: this doesn't handle filter colors
            if (renderData.RenderColor == Vector3.Zero)
            {
                return new Vector3(colorMultiplier * 255 / 256);
            }

            var color = renderData.RenderColor;

            //This replicates a bug in the engine that causes Color render mode to ignore render mode
            //This way the color from the sprite is retained
            if (renderData.RenderMode == RenderMode.TransColor)
            {
                color = Vector3.One * 255;
            }

            color *= colorMultiplier;
            color /= 256.0f;

            //TODO: handle filtering
            //TODO: should probably be a post processing effect
            /*
            if (filterMode)
            {
                color *= filterColor;
            }
            */

            return color;
        }

        private int GlowBlend(ref ModelRenderData renderData, IViewState viewState)
        {
            var tmp = renderData.Origin - viewState.Origin;

            var distance = tmp.Length();

            //TODO: implement trace

            /*
            pmove->usehull = 2;

            var traceFlags = PM_GLASS_IGNORE;

            if (!r_traceglow.value)
            {
                traceFlags |= PM_STUDIO_IGNORE;
            }

            var trace = pmove->PM_PlayerTrace(viewState.Origin, renderData.Origin, traceFlags, -1);

            if ((1.0 - trace.fraction) * distance > 8.0)
            {
                return 0;
            }
            else */
            if (renderData.RenderFX == RenderFX.NoDissipation)
            {
                return renderData.RenderAmount;
            }
            else
            {
                renderData.Scale = new Vector3(distance * 0.005f);

                return (int)(Math.Clamp(19000.0f / (distance * distance), 0.5f, 1.0f) * 255);
            }
        }

        private Vector3 GetSpriteAngles(ref ModelRenderData renderData, SpriteType type, IViewState viewState)
        {
            //Convert parallel sprites to parallel oriented if a roll was specified
            if (type == SpriteType.Parallel && renderData.Angles.Z != 0)
            {
                type = SpriteType.ParallelOriented;
            }

            Vector3 GetModifiedViewAngles()
            {
                var angles = viewState.Angles;

                //Pitch and roll need to be inverted to operate in the sprite's coordinate system
                //Yaw stays the same
                angles.X = -angles.X;
                angles.Z = -angles.Z;

                return angles;
            }

            switch (type)
            {
                case SpriteType.FacingUpright:
                    {
                        //This is bugged in vanilla since it uses an origin that isn't initialized by sprite models, only other models
                        var angles = VectorUtils.VectorToAngles(-renderData.Origin);

                        //No pitch
                        angles.X = 0;

                        return angles;
                    }

                case SpriteType.ParallelUpright:
                    {
                        var angles = GetModifiedViewAngles();

                        //No pitch
                        angles.X = 0;

                        return angles;
                    }

                case SpriteType.Parallel:
                    {
                        return GetModifiedViewAngles();
                    }

                case SpriteType.Oriented:
                    {
                        return renderData.Angles;
                    }

                case SpriteType.ParallelOriented:
                    {
                        var angles = GetModifiedViewAngles();

                        //Apply roll
                        angles.Z -= renderData.Angles.Z;

                        return angles;
                    }

                default:
                    {
                        _factory.Logger.Warning($"{nameof(GetSpriteAngles)}: Bad sprite type {type}");
                        break;
                    }
            }

            return new Vector3();
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
            if (renderData.RenderMode == RenderMode.Glow)
            {
                renderData.RenderAmount = GlowBlend(ref renderData, sc.ViewState);
            }

            //Don't render if blend is 0 (even if blend were ignored below)
            if (renderData.RenderAmount == 0)
            {
                return;
            }

            var blend = renderData.RenderMode != RenderMode.Normal ? renderData.RenderAmount : 255;

            //TODO: glow sprite visibility testing
            var angles = GetSpriteAngles(ref renderData, _spriteModel.SpriteFile.Type, sc.ViewState);

            angles = VectorUtils.ToRadians(angles);

            var wai = new WorldAndInverse();

            var anglesWithoutYaw = angles;

            anglesWithoutYaw.Y = 0;

            wai.World = Matrix4x4.CreateScale(renderData.Scale)
                * WorldAndInverse.CreateRotationMatrix(anglesWithoutYaw)
                * WorldAndInverse.CreateRotationMatrix(new Vector3(0, angles.Y, 0))
                * Matrix4x4.CreateTranslation(renderData.Origin);

            wai.InverseWorld = VdUtilities.CalculateInverseTranspose(ref wai.World);

            //var wai = new WorldAndInverse(renderData.Origin, angles, renderData.Scale);

            cl.UpdateBuffer(_worldAndInverseBuffer, 0, ref wai);

            var alpha = 255;

            switch (renderData.RenderMode)
            {
                case RenderMode.Normal:
                case RenderMode.TransTexture:
                case RenderMode.TransAlpha:
                    alpha = blend;
                    break;
            }

            var renderColor = new Vector4(CalculateSpriteColor(ref renderData, blend), alpha) / 255.0f;

            cl.UpdateBuffer(_renderColorBuffer, 0, ref renderColor);

            renderData.Frame = Math.Clamp((int)renderData.Frame, 0, _frameBuffers.Count - 1);

            var frameBuffer = _frameBuffers[(int)renderData.Frame];

            var pipeline = _factory.Pipelines[renderData.RenderMode];

            cl.SetVertexBuffer(0, frameBuffer);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, _resourceSet);
            cl.DrawIndexed((uint)Indices.Length, 1, 0, 0, 0);
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            var atlasImage = CreateSpriteAtlas(gd, disposeFactory, sc);

            //TODO: disable mipmapping for certain sprites?
            var texture = sc.MapResourceCache.AddTexture2D(gd, disposeFactory, new ImageSharpTexture(atlasImage, true), $"{_spriteModel.Name}_atlas");

            _ib = disposeFactory.CreateBuffer(new BufferDescription(Indices.SizeInBytes(), BufferUsage.IndexBuffer));

            gd.UpdateBuffer(_ib, 0, Indices);

            _worldAndInverseBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<WorldAndInverse>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            _renderColorBuffer = disposeFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<Vector4>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var view = sc.MapResourceCache.GetTextureView(gd.ResourceFactory, texture);

            _resourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _factory.Layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                _worldAndInverseBuffer,
                view,
                sc.MainSampler,
                sc.LightingInfoBuffer,
                _renderColorBuffer));
        }

        private Image<Rgba32> CreateSpriteAtlas(GraphicsDevice gd, DisposeCollectorResourceFactory disposeFactory, SceneContext sc)
        {
            //Merge all of the sprite frames together into one texture
            //The sprite's bounds are the maximum size that a frame can be
            //Since most sprites have identical frame sizes, this lets us optimize pretty well

            //Determine how many frames to put on one line
            //This helps optimize texture size
            var framesPerLine = (int)Math.Ceiling(Math.Sqrt(_spriteModel.SpriteFile.Frames.Count));

            //Account for the change in size when converting textures
            (var maximumWith, var maximumHeight) = sc.TextureLoader.ComputeScaledSize(_spriteModel.SpriteFile.MaximumWidth, _spriteModel.SpriteFile.MaximumHeight);

            var totalWidth = maximumWith * framesPerLine;
            var totalHeight = maximumHeight * framesPerLine;

            var atlasImage = new Image<Rgba32>(totalWidth, totalHeight);

            var graphicsOptions = GraphicsOptions.Default;

            graphicsOptions.BlenderMode = PixelBlenderMode.Src;

            var nextFramePosition = new Point();

            //Determine which texture format it is
            TextureFormat textureFormat;

            switch (_spriteModel.SpriteFile.TextureFormat)
            {
                case SpriteTextureFormat.Normal:
                case SpriteTextureFormat.Additive:
                    textureFormat = TextureFormat.Normal;
                    break;
                case SpriteTextureFormat.AlphaTest:
                    textureFormat = TextureFormat.AlphaTest;
                    break;
                default:
                    textureFormat = TextureFormat.IndexAlpha;
                    break;
            }

            foreach (var frame in _spriteModel.SpriteFile.Frames)
            {
                //Each individual texture is converted before being added to the atlas to avoid bleeding effects between frames
                var frameImage = sc.TextureLoader.ConvertTexture(
                    new IndexedColor256Texture(_spriteModel.SpriteFile.Palette, frame.TextureData, frame.Area.Width, frame.Area.Height),
                    textureFormat);

                atlasImage.Mutate(context => context.DrawImage(graphicsOptions, frameImage, nextFramePosition));

                //Note: The frame origin does not apply to texture coordinates, only to vertex offsets
                //Important! these are float types
                PointF frameOrigin = nextFramePosition;

                var frameSize = new SizeF(frameImage.Width, frameImage.Height);

                //Convert to texture coordinates
                frameOrigin.X /= totalWidth;
                frameOrigin.Y /= totalHeight;

                frameSize.Width /= totalWidth;
                frameSize.Height /= totalHeight;

                //The vertices should be scaled to match the frame size
                //These don't need to be modified to account for texture scale!
                var scale = new Vector3(0, frame.Area.Width, frame.Area.Height);
                var translation = new Vector3(0, frame.Area.X, frame.Area.Y);

                //Construct the vertices
                var vertices = new WorldTextureCoordinate[]
                {
                    new WorldTextureCoordinate
                    {
                        Vertex = (Vertices[0] * scale) + translation,
                        Texture = new Vector2(frameOrigin.X, frameOrigin.Y)
                    },
                    new WorldTextureCoordinate
                    {
                        Vertex = (Vertices[1] * scale) + translation,
                        Texture = new Vector2(frameOrigin.X + frameSize.Width, frameOrigin.Y)
                    },
                    new WorldTextureCoordinate
                    {
                        Vertex = (Vertices[2] * scale) + translation,
                        Texture = new Vector2(frameOrigin.X + frameSize.Width, frameOrigin.Y + frameSize.Height)
                    }
                    ,
                    new WorldTextureCoordinate
                    {
                        Vertex = (Vertices[3] * scale) + translation,
                        Texture = new Vector2(frameOrigin.X, frameOrigin.Y + frameSize.Height)
                    }
                };

                var vb = disposeFactory.CreateBuffer(new BufferDescription(vertices.SizeInBytes(), BufferUsage.VertexBuffer));

                gd.UpdateBuffer(vb, 0, vertices);

                _frameBuffers.Add(vb);

                nextFramePosition.X += maximumWith;

                //Wrap to next line
                if (nextFramePosition.X >= totalWidth)
                {
                    nextFramePosition.X = 0;
                    nextFramePosition.Y += maximumHeight;
                }
            }

            return atlasImage;
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
            _frameBuffers.Clear();
        }

        private static readonly Vector3[] Vertices = new Vector3[]
        {
            //TODO: verify that these are correct
            //Upper left
            new Vector3(0, 1, 1),
            //Upper right
            new Vector3(0, 0, 1),
            //Lower right
            new Vector3(0, 0, 0),
            //Lower left
            new Vector3(0, 1, 0),
        };

        private static readonly ushort[] Indices = new ushort[]
        {
            0, 1, 2,
            2, 3, 0
        };
    }
}
