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

using Force.Crc32;
using SharpLife.FileFormats.BSP.Disk;
using SharpLife.FileFormats.WAD;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.FileFormats.BSP
{
    public static class Input
    {
        private const int FaceSize = 20;

        /// <summary>
        /// Reads the header of a BSP file
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Header ReadHeader(BinaryReader reader)
        {
            var version = EndianConverter.Little(reader.ReadInt32());

            //Verify that we can load this BSP file
            if (!Enum.IsDefined(typeof(BSPVersion), version))
            {
                throw new InvalidBSPVersionException(version);
            }

            var header = new Header
            {
                Version = (BSPVersion)version
            };

            var lumps = new Lump[(int)LumpId.LastLump + 1];

            foreach (var i in Enumerable.Range((int)LumpId.FirstLump, (int)LumpId.LastLump + 1))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Lump>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                lumps[i] = Marshal.PtrToStructure<Lump>(handle.AddrOfPinnedObject());
                lumps[i].fileofs = EndianConverter.Little(lumps[i].fileofs);
                lumps[i].filelen = EndianConverter.Little(lumps[i].filelen);
                handle.Free();
            }

            header.Lumps = lumps;

            return header;
        }

        private static List<MipTexture> ReadMipTextures(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = EndianConverter.Little(reader.ReadInt32());

            var textureOffsets = new int[count];

            foreach (var i in Enumerable.Range(0, count))
            {
                textureOffsets[i] = EndianConverter.Little(reader.ReadInt32());
            }

            var textures = new List<MipTexture>(count);

            foreach (var textureOffset in textureOffsets)
            {
                textures.Add(WAD.Input.ReadMipTexture(reader, lump.fileofs + textureOffset));
            }

            return textures;
        }

        private static List<Plane> ReadPlanes(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Plane>();

            var planes = new List<Plane>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Disk.Plane>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var plane = Marshal.PtrToStructure<Disk.Plane>(handle.AddrOfPinnedObject());
                handle.Free();

                plane.Normal = EndianTypeConverter.Little(plane.Normal);
                plane.Distance = EndianConverter.Little(plane.Distance);
                plane.Type = (PlaneType)EndianConverter.Little((int)plane.Type);

                planes.Add(new Plane { Data = plane });
            }

            return planes;
        }

        private static unsafe void ReadBaseNode(IReadOnlyList<Plane> planes, ref Disk.BaseNode input, BaseNode output)
        {
            output.Plane = planes[EndianConverter.Little(input.planenum)];
            output.Children[0] = EndianConverter.Little(input.children[0]);
            output.Children[1] = EndianConverter.Little(input.children[1]);
        }

        private static unsafe List<Node> ReadNodes(BinaryReader reader, ref Lump lump, IReadOnlyList<Plane> planes, IReadOnlyList<Face> faces)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Node>();

            var nodes = new List<Node>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Disk.Node>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var node = Marshal.PtrToStructure<Disk.Node>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new Node
                {
                    Mins = new Vector3(
                        EndianConverter.Little(node.mins[0]),
                        EndianConverter.Little(node.mins[1]),
                        EndianConverter.Little(node.mins[2])),
                    Maxs = new Vector3(
                        EndianConverter.Little(node.maxs[0]),
                        EndianConverter.Little(node.maxs[1]),
                        EndianConverter.Little(node.maxs[2])),
                };

                ReadBaseNode(planes, ref node.Data, result);

                node.firstface = EndianConverter.Little(node.firstface);
                node.numfaces = EndianConverter.Little(node.numfaces);

                result.Faces = new List<Face>(node.numfaces);

                foreach (var face in Enumerable.Range(node.firstface, node.numfaces))
                {
                    result.Faces.Add(faces[face]);
                }

                nodes.Add(result);
            }

            return nodes;
        }

        private static List<ClipNode> ReadClipNodes(BinaryReader reader, ref Lump lump, IReadOnlyList<Plane> planes)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.ClipNode>();

            var nodes = new List<ClipNode>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Disk.ClipNode>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var node = Marshal.PtrToStructure<Disk.ClipNode>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new ClipNode();

                ReadBaseNode(planes, ref node.Data, result);

                nodes.Add(result);
            }

            return nodes;
        }

        private static unsafe List<TextureInfo> ReadTextureInfos(BinaryReader reader, ref Lump lump, List<MipTexture> mipTextures)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.TextureInfo>();

            var textureInfos = new List<TextureInfo>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Disk.TextureInfo>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var info = Marshal.PtrToStructure<Disk.TextureInfo>(handle.AddrOfPinnedObject());
                textureInfos.Add(new TextureInfo
                {
                    SNormal = EndianTypeConverter.Little(new Vector3(info.vecs[0], info.vecs[1], info.vecs[2])),
                    SValue = EndianConverter.Little(info.vecs[3]),
                    TNormal = EndianTypeConverter.Little(new Vector3(info.vecs[4], info.vecs[5], info.vecs[6])),
                    TValue = EndianConverter.Little(info.vecs[7]),
                    MipTexture = mipTextures[EndianConverter.Little(info.miptex)],
                    Flags = (TextureFlags)EndianConverter.Little((int)info.flags)
                });
                handle.Free();
            }

            return textureInfos;
        }

        private static List<Vector3> ReadVertexes(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Vector3>();

            var vertexes = new List<Vector3>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Vector3>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var vertex = Marshal.PtrToStructure<Vector3>(handle.AddrOfPinnedObject());
                handle.Free();

                vertexes.Add(EndianTypeConverter.Little(vertex));
            }

            return vertexes;
        }

        private static List<Edge> ReadEdges(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Edge>();

            var edges = new List<Edge>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Edge>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var edge = Marshal.PtrToStructure<Edge>(handle.AddrOfPinnedObject());
                handle.Free();

                edge.start = EndianConverter.Little(edge.start);
                edge.end = EndianConverter.Little(edge.end);

                edges.Add(edge);
            }

            return edges;
        }

        private static List<int> ReadSurfEdges(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<int>();

            var surfEdges = new List<int>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                surfEdges.Add(EndianConverter.Little(reader.ReadInt32()));
            }

            return surfEdges;
        }

        private static List<Face> ReadFaces(BinaryReader reader, ref Lump lump,
            IReadOnlyList<Plane> planes,
            IReadOnlyList<Vector3> vertexes,
            IReadOnlyList<Edge> edges,
            IReadOnlyList<int> surfEdges,
            IReadOnlyList<TextureInfo> textureInfos)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / FaceSize;

            var faces = new List<Face>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var face = new Face();

                var planeNumber = reader.ReadInt16();

                face.Plane = planes[EndianConverter.Little(planeNumber)];
                face.Side = EndianConverter.Little(reader.ReadInt16()) != 0;

                var firstEdge = EndianConverter.Little(reader.ReadInt32());
                var numEdges = EndianConverter.Little(reader.ReadInt16());

                face.Points = new List<Vector3>(numEdges);

                for (var edge = 0; edge < numEdges; ++edge)
                {
                    //Surfedge indices can be negative
                    var edgeIndex = surfEdges[firstEdge + edge];

                    var edgeData = edges[Math.Abs(edgeIndex)];

                    face.Points.Add(vertexes[edgeIndex > 0 ? edgeData.start : edgeData.end]);
                }

                var texInfo = EndianConverter.Little(reader.ReadInt16());

                face.TextureInfo = textureInfos[texInfo];

                face.Styles = new byte[Constants.MaxLightmaps];

                foreach (var style in Enumerable.Range(0, Constants.MaxLightmaps))
                {
                    face.Styles[style] = reader.ReadByte();
                }

                face.LightOffset = EndianConverter.Little(reader.ReadInt32());

                faces.Add(face);
            }

            return faces;
        }

        private static List<int> ReadMarkSurfaces(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<ushort>();

            var markSurfaces = new List<int>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                markSurfaces.Add(EndianConverter.Little(reader.ReadUInt16()));
            }

            return markSurfaces;
        }

        private static unsafe List<Leaf> ReadLeafs(BinaryReader reader, ref Lump lump, IReadOnlyList<int> markSurfaces, IReadOnlyList<Face> faces)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Leaf>();

            var leaves = new List<Leaf>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Disk.Leaf>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var leaf = Marshal.PtrToStructure<Disk.Leaf>(handle.AddrOfPinnedObject());
                handle.Free();

                leaf.firstmarksurface = EndianConverter.Little(leaf.firstmarksurface);
                leaf.nummarksurfaces = EndianConverter.Little(leaf.nummarksurfaces);

                var leafFaces = new List<Face>(leaf.nummarksurfaces);

                foreach (var surface in Enumerable.Range(leaf.firstmarksurface, leaf.nummarksurfaces))
                {
                    leafFaces.Add(faces[markSurfaces[surface]]);
                }

                var result = new Leaf
                {
                    Contents = (Contents)EndianConverter.Little((int)leaf.contents),
                    Maxs = new Vector3(
                        EndianConverter.Little(leaf.maxs[0]),
                        EndianConverter.Little(leaf.maxs[1]),
                        EndianConverter.Little(leaf.maxs[2])),
                    Mins = new Vector3(
                        EndianConverter.Little(leaf.mins[0]),
                        EndianConverter.Little(leaf.mins[1]),
                        EndianConverter.Little(leaf.mins[2])),
                    Faces = leafFaces,
                    VisOffset = EndianConverter.Little(leaf.visofs)
                };

                foreach (var ambient in Enumerable.Range(0, (int)Ambient.LastAmbient + 1))
                {
                    result.AmbientLevel[ambient] = leaf.ambient_level[ambient];
                }

                leaves.Add(result);
            }

            return leaves;
        }

        private static unsafe List<Model> ReadModels(BinaryReader reader, ref Lump lump, IReadOnlyList<Face> faces)
        {
            reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Model>();

            var models = new List<Model>();

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = reader.ReadBytes(Marshal.SizeOf<Disk.Model>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var model = Marshal.PtrToStructure<Disk.Model>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new Model
                {
                    Mins = model.mins,
                    Maxs = model.maxs,
                    Origin = model.origin,
                    NumVisLeaves = model.visleafs
                };

                foreach (var node in Enumerable.Range(0, Constants.MaxHulls))
                {
                    result.HeadNodes[node] = model.headnode[node];
                }

                result.Faces = new List<Face>(model.numfaces);

                foreach (var face in Enumerable.Range(model.firstface, model.numfaces))
                {
                    result.Faces.Add(faces[face]);
                }

                models.Add(result);
            }

            return models;
        }

        private static string ReadEntities(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            var rawBytes = reader.ReadBytes(lump.filelen);

            return Encoding.UTF8.GetString(rawBytes);
        }

        private static byte[] ReadVisibility(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            return reader.ReadBytes(lump.filelen);
        }

        private static byte[] ReadLighting(BinaryReader reader, ref Lump lump)
        {
            reader.BaseStream.Position = lump.fileofs;

            return reader.ReadBytes(lump.filelen);
        }

        public static BSPFile ReadBSPFile(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var header = ReadHeader(reader);

            //Determine if this is a Blue Shift BSP file
            //This works by checking if the planes lump actually contains planes
            //It can only contain planes if the length is a multiple of the plane data structure size,
            //AND the entities lump is a multiple of the size
            //These 2 lumps are switched in those BSP files
            var isBlueShiftBSP = IsBlueShiftBSP(header);

            var entitiesLumpId = isBlueShiftBSP ? LumpId.Planes : LumpId.Entities;
            var planesLumpId = isBlueShiftBSP ? LumpId.Entities : LumpId.Planes;

            var mipTextures = ReadMipTextures(reader, ref header.Lumps[(int)LumpId.Textures]);
            var planes = ReadPlanes(reader, ref header.Lumps[(int)planesLumpId]);

            var textureInfos = ReadTextureInfos(reader, ref header.Lumps[(int)LumpId.TexInfo], mipTextures);

            var vertexes = ReadVertexes(reader, ref header.Lumps[(int)LumpId.Vertexes]);
            var edges = ReadEdges(reader, ref header.Lumps[(int)LumpId.Edges]);
            var surfEdges = ReadSurfEdges(reader, ref header.Lumps[(int)LumpId.SurfEdges]);
            var faces = ReadFaces(reader, ref header.Lumps[(int)LumpId.Faces], planes, vertexes, edges, surfEdges, textureInfos);

            var nodes = ReadNodes(reader, ref header.Lumps[(int)LumpId.Nodes], planes, faces);
            var clipNodes = ReadClipNodes(reader, ref header.Lumps[(int)LumpId.ClipNodes], planes);

            var markSurfaces = ReadMarkSurfaces(reader, ref header.Lumps[(int)LumpId.MarkSurfaces]);

            var leaves = ReadLeafs(reader, ref header.Lumps[(int)LumpId.Leafs], markSurfaces, faces);

            var models = ReadModels(reader, ref header.Lumps[(int)LumpId.Models], faces);

            var entities = ReadEntities(reader, ref header.Lumps[(int)entitiesLumpId]);

            var visibility = ReadVisibility(reader, ref header.Lumps[(int)LumpId.Visibility]);

            var lighting = ReadLighting(reader, ref header.Lumps[(int)LumpId.Lighting]);

            var bspFile = new BSPFile
            {
                Version = header.Version,
                MipTextures = mipTextures,
                Planes = planes,
                Faces = faces,
                Leaves = leaves,
                Models = models,
                Nodes = nodes,
                ClipNodes = clipNodes,
                Entities = entities,
                Visibility = visibility,
                Lighting = lighting,
                HasBlueShiftLumpLayout = isBlueShiftBSP
            };

            return bspFile;
        }

        public static BSPFile ReadBSPFile(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                return ReadBSPFile(reader);
            }
        }

        private static bool IsBlueShiftBSP(Header header)
        {
            //Determine if this is a Blue Shift BSP file
            //This works by checking if the planes lump actually contains planes
            //It can only contain planes if the length is a multiple of the plane data structure size,
            //AND the entities lump is a multiple of the size
            //These 2 lumps are switched in those BSP files
            return (header.Lumps[(int)LumpId.Planes].filelen % Marshal.SizeOf<Disk.Plane>()) != 0
                && (header.Lumps[(int)LumpId.Entities].filelen % Marshal.SizeOf<Disk.Plane>()) == 0;
        }

        /// <summary>
        /// Identifies whether the given reader contains a Blue Shift BSP file
        /// </summary>
        /// <param name="reader"></param>
        public static bool IsBlueShiftBSP(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var position = reader.BaseStream.Position;

            try
            {
                var header = ReadHeader(reader);

                return IsBlueShiftBSP(header);
            }
            finally
            {
                //Restore original position since this is a query operation
                reader.BaseStream.Position = position;
            }
        }

        /// <summary>
        /// <see cref="IsBlueShiftBSP(BinaryReader)"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool IsBlueShiftBSP(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                return IsBlueShiftBSP(new BinaryReader(stream));
            }
        }

        public static uint ComputeCRC(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var header = ReadHeader(reader);

                var isBlueShiftBSP = IsBlueShiftBSP(header);

                var ignoreLump = isBlueShiftBSP ? LumpId.Planes : LumpId.Entities;

                uint crc = 0;

                //Append each lump to CRC, except entities lump since servers should be able to run Ripented maps

                var buffer = new byte[1024];

                foreach (var i in Enumerable.Range((int)LumpId.FirstLump, (LumpId.LastLump - LumpId.FirstLump) + 1))
                {
                    if (i == (int)ignoreLump)
                    {
                        continue;
                    }

                    reader.BaseStream.Position = header.Lumps[i].fileofs;

                    var bytesLeft = header.Lumps[i].filelen;

                    while (bytesLeft > 0)
                    {
                        var bytesToRead = bytesLeft < buffer.Length ? bytesLeft : buffer.Length;

                        var bytesRead = reader.Read(buffer, 0, bytesToRead);

                        if (bytesRead != bytesToRead)
                        {
                            var totalRead = header.Lumps[i].filelen - bytesLeft - (bytesToRead - bytesRead);
                            throw new InvalidOperationException($"BSP lump {i} has invalid file length data, expected {header.Lumps[i].filelen}, got {totalRead}");
                        }

                        crc = Crc32Algorithm.Append(crc, buffer, 0, bytesToRead);

                        bytesLeft -= bytesToRead;
                    }
                }

                return crc;
            }
        }
    }
}
