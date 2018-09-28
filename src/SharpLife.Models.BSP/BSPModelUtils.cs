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

using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.BSP.Rendering;
using SharpLife.Utility.FileSystem;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SharpLife.Models.BSP
{
    public sealed class BSPModelUtils
    {
        private readonly string _bspModelNamePrefix;

        private readonly string _mapsDirectory;

        private readonly string _bspExtension;

        private readonly string _mapFileNameBaseRegexString;

        public BSPModelUtils(string bspModelNamePrefix, string mapsDirectory, string bspExtension)
        {
            _bspModelNamePrefix = bspModelNamePrefix ?? throw new ArgumentNullException(nameof(bspModelNamePrefix));
            _mapsDirectory = mapsDirectory ?? throw new ArgumentNullException(nameof(mapsDirectory));
            _bspExtension = bspExtension ?? throw new ArgumentNullException(nameof(bspExtension));

            _mapFileNameBaseRegexString =
            mapsDirectory
            + $"[{Regex.Escape(Path.DirectorySeparatorChar.ToString() + Path.AltDirectorySeparatorChar.ToString())}](\\w+)"
            + Regex.Escape(FileExtensionUtils.AsExtension(bspExtension));
        }

        public bool IsBSPModelName(string modelName)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            return modelName.StartsWith(_bspModelNamePrefix);
        }

        /// <summary>
        /// Formats a map name as a file name
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public string FormatMapFileName(string mapName)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            return Path.Combine(_mapsDirectory, mapName + FileExtensionUtils.AsExtension(_bspExtension));
        }

        /// <summary>
        /// Extracts the base map name from a file name
        /// </summary>
        /// <param name="mapFileName"></param>
        /// <returns></returns>
        /// <exception cref="FormatException">If the file name is not a map file name</exception>
        public string ExtractMapBaseName(string mapFileName)
        {
            if (mapFileName == null)
            {
                throw new ArgumentNullException(nameof(mapFileName));
            }

            var match = Regex.Match(mapFileName, _mapFileNameBaseRegexString);

            if (!match.Success)
            {
                throw new FormatException($"Could not extract map base name from {mapFileName}");
            }

            return match.Groups[1].Captures[0].Value;
        }

        /// <summary>
        /// Gets the surface at the given point in the world
        /// </summary>
        /// <param name="model"></param>
        /// <param name="baseNode"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Face SurfaceAtPoint(Model model, BaseNode baseNode, in Vector3 start, in Vector3 end)
        {
            //TODO: using "as" might be cheaper here since we have to cast anyway
            if (baseNode.Contents < Contents.Node)
            {
                return null;
            }

            var node = (Node)baseNode;

            var front = Vector3.Dot(start, node.Plane.Normal) - node.Plane.Distance;
            var back = Vector3.Dot(end, node.Plane.Normal) - node.Plane.Distance;

            var side = 0.0 > front;

            if ((0.0 > back) == side)
            {
                return SurfaceAtPoint(model, node.Children[side ? 1 : 0], start, end);
            }

            var fraction = front / (front - back);

            var mid = start + ((end - start) * fraction);

            {
                var candidateSurface = SurfaceAtPoint(model, node.Children[side ? 1 : 0], start, mid);

                if (candidateSurface != null)
                {
                    return candidateSurface;
                }
            }

            foreach (var surface in node.Faces)
            {
                if ((surface.Flags & FaceFlags.Tiled) == 0)
                {
                    var xPos = (int)(Vector3.Dot(surface.TextureInfo.SNormal, mid) + surface.TextureInfo.SValue);

                    if (xPos >= surface.TextureMins[0])
                    {
                        var yPos = (int)(Vector3.Dot(surface.TextureInfo.TNormal, mid) + surface.TextureInfo.TValue);

                        if (yPos >= surface.TextureMins[1])
                        {
                            var width = xPos - surface.TextureMins[0];

                            if (width <= surface.Extents[0])
                            {
                                var height = yPos - surface.TextureMins[1];

                                if (height <= surface.Extents[1])
                                {
                                    return surface;
                                }
                            }
                        }
                    }
                }
            }

            return SurfaceAtPoint(model, node.Children[side ? 0 : 1], mid, end);
        }

        private static Rgb24 InternalRecursiveLightPoint(Model model, Rgb24[] lightData, LightStyles lightStyles, BaseNode baseNode, in Vector3 start, in Vector3 end)
        {
            //TODO: using "as" might be cheaper here since we have to cast anyway
            if (baseNode.Contents < Contents.Node)
            {
                return new Rgb24(0, 0, 0);
            }

            var node = (Node)baseNode;

            var front = Vector3.Dot(start, node.Plane.Normal) - node.Plane.Distance;
            var back = Vector3.Dot(end, node.Plane.Normal) - node.Plane.Distance;

            var side = 0.0 > front;

            if ((0.0 > back) == side)
            {
                return InternalRecursiveLightPoint(model, lightData, lightStyles, node.Children[side ? 1 : 0], start, end);
            }

            var fraction = front / (front - back);

            var mid = start + ((end - start) * fraction);

            var candidateColor = InternalRecursiveLightPoint(model, lightData, lightStyles, node.Children[side ? 1 : 0], start, mid);

            if (candidateColor.R != 0 || candidateColor.G != 0 || candidateColor.B != 0)
            {
                return candidateColor;
            }

            foreach (var surface in node.Faces)
            {
                if ((surface.Flags & FaceFlags.Tiled) == 0)
                {
                    var xPos = (int)(Vector3.Dot(surface.TextureInfo.SNormal, mid) + surface.TextureInfo.SValue);

                    if (xPos >= surface.TextureMins[0])
                    {
                        var yPos = (int)(Vector3.Dot(surface.TextureInfo.TNormal, mid) + surface.TextureInfo.TValue);

                        if (yPos >= surface.TextureMins[1])
                        {
                            var width = xPos - surface.TextureMins[0];

                            if (width <= surface.Extents[0])
                            {
                                var height = yPos - surface.TextureMins[1];

                                if (height <= surface.Extents[1])
                                {
                                    if (surface.LightOffset == -1)
                                    {
                                        return candidateColor;
                                    }

                                    var xOffset = (surface.Extents[0] / BSPConstants.LightmapScale) + 1;
                                    var sampleOffset = ((surface.Extents[1] / BSPConstants.LightmapScale) + 1) * xOffset;

                                    var sampleIndex = surface.LightOffset + (width / BSPConstants.LightmapScale) + (xOffset * (height / BSPConstants.LightmapScale));

                                    int r = 0, g = 0, b = 0;

                                    for (int styleIndex = 0; styleIndex < BSPConstants.MaxLightmaps && surface.Styles[styleIndex] != BSPConstants.NoLightStyle; ++styleIndex)
                                    {
                                        var styleValue = lightStyles.GetStyleValue(surface.Styles[styleIndex]);

                                        ref var sample = ref lightData[sampleIndex];

                                        r += sample.R * styleValue;
                                        g += sample.G * styleValue;
                                        b += sample.B * styleValue;

                                        sampleIndex += sampleOffset;
                                    }

                                    r /= 256;
                                    g /= 256;
                                    b /= 256;

                                    if (r <= 0)
                                        r = 1;

                                    return new Rgb24((byte)r, (byte)g, (byte)b);
                                }
                            }
                        }
                    }
                }
            }

            return InternalRecursiveLightPoint(model, lightData, lightStyles, node.Children[side ? 0 : 1], mid, end);
        }

        /// <summary>
        /// Gets the color value on the surface that the given trace intersects, or black if there is no surface, or if the surface has no light data
        /// </summary>
        /// <param name="model">BSP model to operate on</param>
        /// <param name="lightData">The BSP file's light data</param>
        /// <param name="lightStyles">The current state of light styles</param>
        /// <param name="baseNode">Node to start traversing at</param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Rgb24 RecursiveLightPoint(Model model, Rgb24[] lightData, LightStyles lightStyles, BaseNode baseNode, in Vector3 start, in Vector3 end)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (lightData == null)
            {
                throw new ArgumentNullException(nameof(lightData));
            }

            if (lightStyles == null)
            {
                throw new ArgumentNullException(nameof(lightStyles));
            }

            if (baseNode == null)
            {
                throw new ArgumentNullException(nameof(baseNode));
            }

            return InternalRecursiveLightPoint(model, lightData, lightStyles, baseNode, start, end);
        }
    }
}
