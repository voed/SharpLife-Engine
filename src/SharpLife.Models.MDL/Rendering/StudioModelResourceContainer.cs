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
using SharpLife.Models.MDL.Loading;
using SharpLife.Renderer;
using SharpLife.Renderer.Models;
using SharpLife.Renderer.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Models.MDL.Rendering
{
    public sealed class StudioModelResourceContainer : ModelResourceContainer
    {
        private struct StudioVertex
        {
            public WorldTextureCoordinate WorldTexture;

            public uint BoneIndex;
        }

        private class MeshData
        {
            public BodyMesh Mesh;

            public uint StartIndex;

            public uint IndicesCount;
        }

        private class SubModelData
        {
            public MeshData[] Meshes;
        }

        private class BodyPartData
        {
            public SubModelData[] SubModels;
        }

        private readonly StudioModelResourceFactory _factory;

        private readonly StudioModel _studioModel;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private ResourceSet _sharedResourceSet;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        private ResourceSet[] _textures;

        private BodyPartData[] _bodyParts;

        public override IModel Model => _studioModel;

        public StudioModelResourceContainer(StudioModelResourceFactory factory, StudioModel studioModel)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _studioModel = studioModel ?? throw new ArgumentNullException(nameof(studioModel));
        }

        //TODO: implement

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass, ref ModelRenderData renderData)
        {
            var wai = new WorldAndInverse(renderData.Origin, renderData.Angles, renderData.Scale);

            cl.UpdateBuffer(_factory.WorldAndInverseBuffer, 0, ref wai);

            var studioRenderData = new StudioModelRenderData
            {
                Frame = renderData.Frame
            };

            var bones = _factory.BoneCalculator.SetUpBones(_studioModel.StudioFile, studioRenderData);

            cl.UpdateBuffer(_factory.BonesBuffer, 0, bones);

            cl.SetPipeline(_factory.Pipeline);

            cl.SetGraphicsResourceSet(0, _sharedResourceSet);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

            foreach (var bodyPart in _bodyParts)
            {
                var subModel = bodyPart.SubModels[0];

                foreach (var mesh in subModel.Meshes)
                {
                    cl.SetGraphicsResourceSet(1, _textures[_studioModel.StudioFile.Skins[0][mesh.Mesh.Skin]]);

                    cl.DrawIndexed(mesh.IndicesCount, 1, mesh.StartIndex, 0, 0);
                }
            }
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            var disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);

            var vertices = new List<StudioVertex>();
            var indices = new List<uint>();

            //Construct the meshes for each body part
            var bodyParts = new List<BodyPartData>(_studioModel.StudioFile.BodyParts.Count);

            foreach (var bodyPart in _studioModel.StudioFile.BodyParts)
            {
                bodyParts.Add(CreateBodyPart(bodyPart, vertices, indices));
            }

            _bodyParts = bodyParts.ToArray();

            var verticesArray = vertices.ToArray();
            var indicesArray = indices.ToArray();

            _vertexBuffer = disposeFactory.CreateBuffer(new BufferDescription(verticesArray.SizeInBytes(), BufferUsage.VertexBuffer));

            gd.UpdateBuffer(_vertexBuffer, 0, verticesArray);

            _indexBuffer = disposeFactory.CreateBuffer(new BufferDescription(indicesArray.SizeInBytes(), BufferUsage.IndexBuffer));

            gd.UpdateBuffer(_indexBuffer, 0, indicesArray);

            _sharedResourceSet = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                _factory.SharedLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                _factory.WorldAndInverseBuffer,
                _factory.BonesBuffer,
                sc.MainSampler
                ));

            CreateTextures(gd, disposeFactory, sc, sc.MapResourceCache);
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Map) == 0)
            {
                return;
            }

            _disposeCollector.DisposeAll();
        }

        private SubModelData CreateSubModel(BodyModel subModel, List<StudioVertex> vertices, List<uint> indices)
        {
            var meshes = new List<MeshData>(subModel.Meshes.Count);

            //Add all vertices to the list
            foreach (var mesh in subModel.Meshes)
            {
                //Use the first skin family for reference
                //The compiler also uses this to calculate the s and t values
                var texture = _studioModel.StudioFile.Textures[_studioModel.StudioFile.Skins[0][mesh.Skin]];

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

        private BodyPartData CreateBodyPart(BodyPart bodyPart, List<StudioVertex> vertices, List<uint> indices)
        {
            var subModels = new List<SubModelData>(bodyPart.Models.Count);

            foreach (var subModel in bodyPart.Models)
            {
                subModels.Add(CreateSubModel(subModel, vertices, indices));
            }

            return new BodyPartData
            {
                SubModels = subModels.ToArray()
            };
        }

        private void CreateTextures(GraphicsDevice gd, ResourceFactory factory, SceneContext sc, ResourceCache cache)
        {
            var textures = new List<ResourceSet>(_studioModel.StudioFile.Textures.Count);

            foreach (var texture in _studioModel.StudioFile.Textures)
            {
                //TODO: disable mipmaps when NoMips is provided
                var uploadedTexture = sc.TextureLoader.LoadTexture(
                        new IndexedColor256Texture(texture.Palette, texture.Pixels, texture.Width, texture.Height),
                        (texture.Flags & TextureFlags.Alpha) != 0 ? TextureFormat.AlphaTest : TextureFormat.Normal,
                        _studioModel.Name + texture.Name,
                        gd,
                        cache);

                var view = cache.GetTextureView(gd.ResourceFactory, uploadedTexture);

                textures.Add(factory.CreateResourceSet(new ResourceSetDescription(_factory.TextureLayout, view)));
            }

            _textures = textures.ToArray();
        }
    }
}
