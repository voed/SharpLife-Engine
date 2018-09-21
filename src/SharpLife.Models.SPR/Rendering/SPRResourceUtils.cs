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

using SharpLife.Models.SPR.FileFormat;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace SharpLife.Models.SPR.Rendering
{
    public static class SPRResourceUtils
    {
        //TODO: use IReadOnlyList for these?
        public static readonly Vector3[] Vertices = new Vector3[]
        {
            //Upper left
            new Vector3(0, 1, 1),
            //Upper right
            new Vector3(0, 0, 1),
            //Lower right
            new Vector3(0, 0, 0),
            //Lower left
            new Vector3(0, 1, 0),
        };

        public static readonly ushort[] Indices = new ushort[]
        {
            0, 1, 2,
            2, 3, 0
        };

        public static (Image<Rgba32>, DeviceBuffer[]) CreateSpriteAtlas(
            SpriteFile spriteFile, GraphicsDevice gd, ResourceFactory disposeFactory, TextureLoader textureLoader)
        {
            //Merge all of the sprite frames together into one texture
            //The sprite's bounds are the maximum size that a frame can be
            //Since most sprites have identical frame sizes, this lets us optimize pretty well

            //Determine how many frames to put on one line
            //This helps optimize texture size
            var framesPerLine = (int)Math.Ceiling(Math.Sqrt(spriteFile.Frames.Count));

            //Account for the change in size when converting textures
            (var maximumWith, var maximumHeight) = textureLoader.ComputeScaledSize(spriteFile.MaximumWidth, spriteFile.MaximumHeight);

            var totalWidth = maximumWith * framesPerLine;
            var totalHeight = maximumHeight * framesPerLine;

            var atlasImage = new Image<Rgba32>(totalWidth, totalHeight);

            var graphicsOptions = GraphicsOptions.Default;

            graphicsOptions.BlenderMode = PixelBlenderMode.Src;

            var nextFramePosition = new Point();

            //Determine which texture format it is
            TextureFormat textureFormat;

            switch (spriteFile.TextureFormat)
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

            var frameBuffers = new List<DeviceBuffer>();

            foreach (var frame in spriteFile.Frames)
            {
                //Each individual texture is converted before being added to the atlas to avoid bleeding effects between frames
                var frameImage = textureLoader.ConvertTexture(
                    new IndexedColor256Image(spriteFile.Palette, frame.TextureData, frame.Area.Width, frame.Area.Height),
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

                //TODO: could refactor this by having one vertex and one index buffer, and returning the start & count of indices for each frame
                frameBuffers.Add(vb);

                nextFramePosition.X += maximumWith;

                //Wrap to next line
                if (nextFramePosition.X >= totalWidth)
                {
                    nextFramePosition.X = 0;
                    nextFramePosition.Y += maximumHeight;
                }
            }

            return (atlasImage, frameBuffers.ToArray());
        }

        public static DeviceBuffer CreateIndexBuffer(GraphicsDevice gd, ResourceFactory factory)
        {
            var ib = factory.CreateBuffer(new BufferDescription(Indices.SizeInBytes(), BufferUsage.IndexBuffer));

            gd.UpdateBuffer(ib, 0, Indices);

            return ib;
        }
    }
}
