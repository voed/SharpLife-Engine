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

using SharpLife.FileFormats.WAD;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Renderer.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace SharpLife.Models.BSP.Rendering
{
    /// <summary>
    /// Utility code to convert BSP data to a more efficient format for rendering
    /// </summary>
    public static class BSPResourceUtils
    {
        private static Image<Rgba32> InternalGenerateLightmap(BSPFile bspFile, Face face, int smax, int tmax, int lightmapIndex)
        {
            var size = smax * tmax;

            var colorData = new Rgba32[size];

            if (bspFile.Lighting.Length > 0 && face.LightOffset != -1)
            {
                //Initialize from light data
                var lightmapData = new Span<Rgb24>(bspFile.Lighting, face.LightOffset + (size * lightmapIndex), size);

                for (var i = 0; i < size; ++i)
                {
                    //Lightmap data is passed directly to shaders; shaders will handle light styles and gamma correction
                    lightmapData[i].ToRgba32(ref colorData[i]);
                }
            }
            else
            {
                //Fill with fullbright
                for (var i = 0; i < size; ++i)
                {
                    colorData[i].R = byte.MaxValue;
                    colorData[i].G = byte.MaxValue;
                    colorData[i].B = byte.MaxValue;
                    colorData[i].A = byte.MaxValue;
                }
            }

            return Image.LoadPixelData(colorData, smax, tmax);
        }

        public static Image<Rgba32> GenerateLightmap(BSPFile bspFile, Face face, int smax, int tmax, int lightmapIndex)
        {
            if (bspFile == null)
            {
                throw new ArgumentNullException(nameof(bspFile));
            }

            if (face == null)
            {
                throw new ArgumentNullException(nameof(face));
            }

            if (smax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(smax));
            }

            if (tmax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tmax));
            }

            return InternalGenerateLightmap(bspFile, face, smax, tmax, lightmapIndex);
        }

        private static Image<Rgba32> InternalCreateLightmapTexture(BSPFile bspFile, Face face, int numLightmaps, int smax, int tmax)
        {
            if (numLightmaps == 0)
            {
                //Let the user decide what to do
                return null;
            }

            var lightmapData = new Image<Rgba32>(numLightmaps * smax, tmax);

            var graphicsOptions = GraphicsOptions.Default;

            graphicsOptions.BlenderMode = PixelBlenderMode.Src;

            //Generate lightmap data for every style
            for (var i = 0; i < numLightmaps; ++i)
            {
                using (var styleData = InternalGenerateLightmap(bspFile, face, smax, tmax, i))
                {
                    lightmapData.Mutate(context => context.DrawImage(graphicsOptions, styleData, new Point(i * smax, 0)));
                }
            }

            return lightmapData;
        }

        /// <summary>
        /// Create the lightmap texture for a surface
        /// If the surface has no lightmaps, returns null
        /// </summary>
        /// <param name="bspFile"></param>
        /// <param name="face"></param>
        /// <param name="numLightmaps"></param>
        /// <param name="smax"></param>
        /// <param name="tmax"></param>
        /// <returns></returns>
        public static Image<Rgba32> CreateLightmapTexture(BSPFile bspFile, Face face, int numLightmaps, int smax, int tmax)
        {
            if (bspFile == null)
            {
                throw new ArgumentNullException(nameof(bspFile));
            }

            if (face == null)
            {
                throw new ArgumentNullException(nameof(face));
            }

            if (numLightmaps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numLightmaps));
            }

            if (smax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(smax));
            }

            if (tmax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tmax));
            }

            return InternalCreateLightmapTexture(bspFile, face, numLightmaps, smax, tmax);
        }

        /// <summary>
        /// Builds the vertices and indices buffers for the given faces
        /// </summary>
        /// <param name="bspFile"></param>
        /// <param name="faces"></param>
        /// <param name="texture"></param>
        /// <param name="textureResourceSet"></param>
        /// <param name="defaultLightmapTexture"></param>
        /// <param name="lightmapBuilders"></param>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        public static void BuildFacesBuffer(
            BSPFile bspFile,
            IReadOnlyList<Face> faces,
            MipTexture texture,
            ResourceSet textureResourceSet,
            Image<Rgba32> defaultLightmapTexture,
            List<LightmapBuilder> lightmapBuilders,
            List<BSPSurfaceData> vertices,
            List<uint> indices)
        {
            if (bspFile == null)
            {
                throw new ArgumentNullException(nameof(bspFile));
            }

            if (faces == null)
            {
                throw new ArgumentNullException(nameof(faces));
            }

            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (textureResourceSet == null)
            {
                throw new ArgumentNullException(nameof(textureResourceSet));
            }

            if (defaultLightmapTexture == null)
            {
                throw new ArgumentNullException(nameof(defaultLightmapTexture));
            }

            if (lightmapBuilders == null)
            {
                throw new ArgumentNullException(nameof(lightmapBuilders));
            }

            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            if (faces.Count == 0)
            {
                throw new ArgumentException("Cannot create a face buffer when no faces are provided", nameof(faces));
            }

            if (lightmapBuilders.Count == 0)
            {
                throw new ArgumentException("You must provide at least one lightmap builder", nameof(lightmapBuilders));
            }

            var lightmapBuilder = lightmapBuilders[lightmapBuilders.Count - 1];

            var firstVertex = vertices.Count;
            var firstIndex = indices.Count;

            void AddTextureData()
            {
                lightmapBuilder.AddTextureData(new SingleTextureData
                {
                    Texture = textureResourceSet,
                    FirstIndex = (uint)firstIndex,
                    IndicesCount = (uint)(indices.Count - firstIndex)
                });
            }

            foreach (var face in faces)
            {
                var smax = (face.Extents[0] / 16) + 1;
                var tmax = (face.Extents[1] / 16) + 1;

                var numLightmaps = face.Styles.Count(style => style != BSPConstants.NoLightStyle);

                using (var lightmapTexture = CreateLightmapTexture(bspFile, face, numLightmaps, smax, tmax))
                {
                    var lightmap = lightmapTexture ?? defaultLightmapTexture;

                    var coordinates = lightmapBuilder.TryAllocate(lightmap);

                    if (!coordinates.HasValue)
                    {
                        //Lightmap is full

                        //Add the current vertices to the full one
                        AddTextureData();

                        //New starting point
                        firstIndex = indices.Count;

                        //Create a new one
                        lightmapBuilder = new LightmapBuilder(lightmapBuilder.Width, lightmapBuilder.Height);
                        lightmapBuilders.Add(lightmapBuilder);

                        //This can't fail without throwing an exception
                        coordinates = lightmapBuilder.TryAllocate(lightmap);
                    }

                    //Create triangles out of the face
                    foreach (var i in Enumerable.Range(vertices.Count + 1, face.Points.Count - 2))
                    {
                        indices.Add((uint)vertices.Count);
                        indices.Add((uint)i);
                        indices.Add((uint)i + 1);
                    }

                    var textureInfo = face.TextureInfo;

                    foreach (var point in face.Points)
                    {
                        var s = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                        s /= texture.Width;

                        var t = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                        t /= texture.Height;

                        var lightmapS = Vector3.Dot(point, textureInfo.SNormal) + textureInfo.SValue;
                        lightmapS -= face.TextureMins[0];
                        lightmapS += coordinates.Value.X * BSPConstants.LightmapScale;
                        lightmapS += 8;
                        lightmapS /= lightmapBuilder.Width * BSPConstants.LightmapScale;
                        //lightmapS /= numLightmaps != 0 ? numLightmaps : 1; //Rescale X so it covers one lightmap in the texture

                        var lightmapT = Vector3.Dot(point, textureInfo.TNormal) + textureInfo.TValue;
                        lightmapT -= face.TextureMins[1];
                        lightmapT += coordinates.Value.Y * BSPConstants.LightmapScale;
                        lightmapT += 8;
                        lightmapT /= lightmapBuilder.Height * BSPConstants.LightmapScale;

                        vertices.Add(new BSPSurfaceData
                        {
                            WorldTexture = new WorldTextureCoordinate
                            {
                                Vertex = point,
                                Texture = new Vector2(s, t)
                            },
                            Lightmap = new Vector2(lightmapS, lightmapT),
                            LightmapXOffset = smax / (float)lightmapBuilder.Width,
                            Style0 = face.Styles[0],
                            Style1 = face.Styles[1],
                            Style2 = face.Styles[2],
                            Style3 = face.Styles[3]
                        });
                    }
                }
            }

            AddTextureData();
        }
    }
}
