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

using SharpLife.Models.MDL.FileFormat;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace SharpLife.Models.MDL.Rendering
{
    public static class StudioResourceUtils
    {
        private static SubModelData InternalCreateSubModel(StudioFile studioFile, BodyModel subModel, List<StudioVertex> vertices, List<uint> indices)
        {
            var meshes = new List<MeshData>(subModel.Meshes.Count);

            //Add all vertices to the list
            foreach (var mesh in subModel.Meshes)
            {
                //Use the first skin family for reference
                //The compiler also uses this to calculate the s and t values
                var texture = studioFile.Textures[studioFile.Skins[0][mesh.Skin]];

                var firstIndex = indices.Count;

                var i = 0;

                for (int command = mesh.TriangleCommands[i]; command != 0; command = mesh.TriangleCommands[i])
                {
                    ++i;

                    //Commands come in sets of 4 values:
                    //Vertex index
                    //Light index, also chrome index for chrome textures
                    //Texture s coord
                    //Texture t coord

                    //Negative values are fans, positive values are strips
                    //TODO: handle chrome
                    var isFan = command < 0;

                    if (isFan)
                    {
                        command = -command;
                    }

                    var firstVertex = vertices.Count;

                    for (var verticesLeft = command; verticesLeft > 0; --verticesLeft, i += 4)
                    {
                        var vertexIndex = mesh.TriangleCommands[i];
                        var lightIndex = mesh.TriangleCommands[i + 1];
                        var sCoord = mesh.TriangleCommands[i + 2];
                        var tCoord = mesh.TriangleCommands[i + 3];

                        vertices.Add(new StudioVertex
                        {
                            WorldTexture = new WorldTextureCoordinate
                            {
                                Vertex = subModel.Vertices[vertexIndex].Vertex,
                                Texture = new Vector2((float)(sCoord / (double)texture.Width), (float)(tCoord / (double)texture.Height))
                            },
                            BoneIndex = (uint)subModel.Vertices[vertexIndex].Bone
                        });
                    }

                    var trianglesToAdd = command - 2;

                    if (isFan)
                    {
                        for (var triangle = 0; triangle < trianglesToAdd; ++triangle)
                        {
                            indices.Add((uint)firstVertex);
                            indices.Add((uint)(firstVertex + triangle + 1));
                            indices.Add((uint)(firstVertex + triangle + 2));
                        }
                    }
                    else
                    {
                        for (var triangle = 0; triangle < trianglesToAdd; ++triangle)
                        {
                            //Every other triangle is inverted because strips normally do that internally
                            if ((triangle % 2) == 0)
                            {
                                indices.Add((uint)firstVertex);
                                indices.Add((uint)(firstVertex + 1));
                                indices.Add((uint)(firstVertex + 2));
                            }
                            else
                            {
                                indices.Add((uint)(firstVertex + 2));
                                indices.Add((uint)(firstVertex + 1));
                                indices.Add((uint)firstVertex);
                            }

                            ++firstVertex;
                        }
                    }
                }

                meshes.Add(new MeshData
                {
                    Mesh = mesh,
                    StartIndex = (uint)firstIndex,
                    IndicesCount = (uint)(indices.Count - firstIndex)
                });
            }

            return new SubModelData
            {
                Meshes = meshes.ToArray()
            };
        }

        public static SubModelData CreateSubModel(StudioFile studioFile, BodyModel subModel, List<StudioVertex> vertices, List<uint> indices)
        {
            if (studioFile == null)
            {
                throw new ArgumentNullException(nameof(studioFile));
            }

            if (subModel == null)
            {
                throw new ArgumentNullException(nameof(subModel));
            }

            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            return InternalCreateSubModel(studioFile, subModel, vertices, indices);
        }

        public static BodyPartData CreateBodyPart(StudioFile studioFile, BodyPart bodyPart, List<StudioVertex> vertices, List<uint> indices)
        {
            if (studioFile == null)
            {
                throw new ArgumentNullException(nameof(studioFile));
            }

            if (bodyPart == null)
            {
                throw new ArgumentNullException(nameof(bodyPart));
            }

            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            var subModels = new List<SubModelData>(bodyPart.Models.Count);

            foreach (var subModel in bodyPart.Models)
            {
                subModels.Add(InternalCreateSubModel(studioFile, subModel, vertices, indices));
            }

            return new BodyPartData
            {
                SubModels = subModels.ToArray()
            };
        }

        public static List<Veldrid.Texture> CreateTextures(string baseName, StudioFile studioFile, GraphicsDevice gd, TextureLoader textureLoader, ResourceCache cache)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (studioFile == null)
            {
                throw new ArgumentNullException(nameof(studioFile));
            }

            if (gd == null)
            {
                throw new ArgumentNullException(nameof(gd));
            }

            if (textureLoader == null)
            {
                throw new ArgumentNullException(nameof(textureLoader));
            }

            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            var textures = new List<Veldrid.Texture>(studioFile.Textures.Count);

            foreach (var texture in studioFile.Textures)
            {
                //TODO: is NoMips incorrectly named?
                var uploadedTexture = textureLoader.LoadTexture(
                        new IndexedColor256Image(texture.Palette, texture.Pixels, texture.Width, texture.Height),
                        (texture.Flags & MDLTextureFlags.Masked) != 0 ? TextureFormat.AlphaTest : TextureFormat.Normal,
                        (texture.Flags & MDLTextureFlags.NoMips) != 0,
                        baseName + texture.Name,
                        gd,
                        cache);

                textures.Add(uploadedTexture);
            }

            return textures;
        }
    }
}
