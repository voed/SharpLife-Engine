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
using SharpLife.FileFormats.WAD;
using SharpLife.Models.BSP.FileFormat.Disk;
using SharpLife.Utility;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.Models.BSP.FileFormat
{
    public class BSPLoader
    {
        private const int FaceSize = 20;

        private readonly BinaryReader _reader;

        private readonly long _startPosition;

        public BSPLoader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _startPosition = _reader.BaseStream.Position;
        }

        public BSPLoader(Stream stream, bool leaveOpen)
            : this(new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, leaveOpen))
        {
        }

        public BSPLoader(Stream stream)
            : this(stream, false)
        {
        }

        public BSPLoader(string fileName)
            : this(File.OpenRead(fileName))
        {
        }

        private static int ReadVersion(BinaryReader reader)
        {
            return EndianConverter.Little(reader.ReadInt32());
        }

        /// <summary>
        /// Returns whether the given reader represents a BSP file
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static bool IsBSPFile(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var originalPosition = reader.BaseStream.Position;

            try
            {
                //Best that can be done to recognize BSP files, since there's no identifier
                return Enum.IsDefined(typeof(BSPVersion), ReadVersion(reader));
            }
            finally
            {
                reader.BaseStream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Reads the header of a BSP file
        /// </summary>
        /// <returns></returns>
        private Header ReadHeader()
        {
            var version = ReadVersion(_reader);

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
                var data = _reader.ReadBytes(Marshal.SizeOf<Lump>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                lumps[i] = Marshal.PtrToStructure<Lump>(handle.AddrOfPinnedObject());
                lumps[i].fileofs = EndianConverter.Little(lumps[i].fileofs);
                lumps[i].filelen = EndianConverter.Little(lumps[i].filelen);
                handle.Free();
            }

            header.Lumps = lumps;

            return header;
        }

        private List<MipTexture> ReadMipTextures(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = EndianConverter.Little(_reader.ReadInt32());

            var textureOffsets = new int[count];

            foreach (var i in Enumerable.Range(0, count))
            {
                textureOffsets[i] = EndianConverter.Little(_reader.ReadInt32());
            }

            var textures = new List<MipTexture>(count);

            foreach (var textureOffset in textureOffsets)
            {
                textures.Add(WADLoader.ReadMipTexture(_reader, lump.fileofs + textureOffset));
            }

            return textures;
        }

        private List<Plane> ReadPlanes(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Plane>();

            var planes = new List<Plane>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Plane>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var plane = Marshal.PtrToStructure<Disk.Plane>(handle.AddrOfPinnedObject());
                handle.Free();

                planes.Add(new Plane
                {
                    Normal = EndianTypeConverter.Little(plane.Normal),
                    Distance = EndianConverter.Little(plane.Distance),
                    Type = (PlaneType)EndianConverter.Little((int)plane.Type)
                });
            }

            return planes;
        }

        private void SetupNodeParent(BaseNode node, BaseNode parent)
        {
            node.Parent = parent;

            if (node.Contents >= Contents.Node)
            {
                var nodeWithChildren = (Node)node;

                SetupNodeParent(nodeWithChildren.Children[0], node);
                SetupNodeParent(nodeWithChildren.Children[1], node);
            }
        }

        private unsafe List<Node> ReadNodes(ref Lump lump, IReadOnlyList<Plane> planes, IReadOnlyList<Face> faces, IReadOnlyList<Leaf> leaves)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Node>();

            var nodes = new List<Node>(count);

            var diskNodes = new List<Disk.Node>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Node>());

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

                result.Plane = planes[EndianConverter.Little(node.Data.planenum)];

                node.Data.children[0] = EndianConverter.Little(node.Data.children[0]);
                node.Data.children[1] = EndianConverter.Little(node.Data.children[1]);

                node.firstface = EndianConverter.Little(node.firstface);
                node.numfaces = EndianConverter.Little(node.numfaces);

                result.Faces = new List<Face>(node.numfaces);

                foreach (var face in Enumerable.Range(node.firstface, node.numfaces))
                {
                    result.Faces.Add(faces[face]);
                }

                nodes.Add(result);
                diskNodes.Add(node);
            }

            //Fix up children
            for (var i = 0; i < count; ++i)
            {
                var diskNode = diskNodes[i];

                var node = nodes[i];

                for (var child = 0; child < 2; ++child)
                {
                    var index = diskNode.Data.children[child];

                    if (index >= 0)
                    {
                        node.Children[child] = nodes[index];
                    }
                    else
                    {
                        node.Children[child] = leaves[~index];
                    }
                }
            }

            //Fix up parents
            SetupNodeParent(nodes[0], null);

            return nodes;
        }

        private unsafe List<ClipNode> ReadClipNodes(ref Lump lump, IReadOnlyList<Plane> planes)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.ClipNode>();

            var nodes = new List<ClipNode>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.ClipNode>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var node = Marshal.PtrToStructure<Disk.ClipNode>(handle.AddrOfPinnedObject());
                handle.Free();

                var result = new ClipNode
                {
                    Plane = planes[EndianConverter.Little(node.Data.planenum)],
                };

                result.Children[0] = EndianConverter.Little(node.Data.children[0]);
                result.Children[1] = EndianConverter.Little(node.Data.children[1]);

                nodes.Add(result);
            }

            return nodes;
        }

        private unsafe List<TextureInfo> ReadTextureInfos(ref Lump lump, List<MipTexture> mipTextures)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.TextureInfo>();

            var textureInfos = new List<TextureInfo>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.TextureInfo>());

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

        private List<Vector3> ReadVertexes(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Vector3>();

            var vertexes = new List<Vector3>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Vector3>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var vertex = Marshal.PtrToStructure<Vector3>(handle.AddrOfPinnedObject());
                handle.Free();

                vertexes.Add(EndianTypeConverter.Little(vertex));
            }

            return vertexes;
        }

        private List<Edge> ReadEdges(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Edge>();

            var edges = new List<Edge>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Edge>());

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var edge = Marshal.PtrToStructure<Edge>(handle.AddrOfPinnedObject());
                handle.Free();

                edge.start = EndianConverter.Little(edge.start);
                edge.end = EndianConverter.Little(edge.end);

                edges.Add(edge);
            }

            return edges;
        }

        private List<int> ReadSurfEdges(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<int>();

            var surfEdges = new List<int>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                surfEdges.Add(EndianConverter.Little(_reader.ReadInt32()));
            }

            return surfEdges;
        }

        private void CalculateExtents(Face face)
        {
            var mins = new Vector2(int.MaxValue, int.MaxValue);
            var maxs = new Vector2(int.MinValue, int.MinValue);

            for (var i = 0; i < face.Points.Count; ++i)
            {
                var point = face.Points[i];

                var sValue = Vector3.Dot(point, face.TextureInfo.SNormal) + face.TextureInfo.SValue;
                var tValue = Vector3.Dot(point, face.TextureInfo.TNormal) + face.TextureInfo.TValue;

                if (sValue < mins.X)
                {
                    mins.X = sValue;
                }

                if (sValue > maxs.X)
                {
                    maxs.X = sValue;
                }

                if (tValue < mins.Y)
                {
                    mins.Y = tValue;
                }

                if (tValue > maxs.Y)
                {
                    maxs.Y = tValue;
                }
            }

            var bmins = new[] { (int)Math.Floor(mins.X / BSPConstants.LightmapScale), (int)Math.Floor(mins.Y / BSPConstants.LightmapScale) };
            var bmaxs = new[] { (int)Math.Ceiling(maxs.X / BSPConstants.LightmapScale), (int)Math.Ceiling(maxs.Y / BSPConstants.LightmapScale) };

            face.Extents[0] = (bmaxs[0] - bmins[0]) * BSPConstants.LightmapScale;
            face.Extents[1] = (bmaxs[1] - bmins[1]) * BSPConstants.LightmapScale;

            face.TextureMins[0] = bmins[0] * BSPConstants.LightmapScale;
            face.TextureMins[1] = bmins[1] * BSPConstants.LightmapScale;
        }

        private List<Face> ReadFaces(ref Lump lump,
            IReadOnlyList<Plane> planes,
            IReadOnlyList<Vector3> vertexes,
            IReadOnlyList<Edge> edges,
            IReadOnlyList<int> surfEdges,
            IReadOnlyList<TextureInfo> textureInfos)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / FaceSize;

            var faces = new List<Face>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var face = new Face();

                var planeNumber = _reader.ReadInt16();

                face.Plane = planes[EndianConverter.Little(planeNumber)];
                face.Side = EndianConverter.Little(_reader.ReadInt16()) != 0;

                var firstEdge = EndianConverter.Little(_reader.ReadInt32());
                var numEdges = EndianConverter.Little(_reader.ReadInt16());

                face.Points = new List<Vector3>(numEdges);

                for (var edge = 0; edge < numEdges; ++edge)
                {
                    //Surfedge indices can be negative
                    var edgeIndex = surfEdges[firstEdge + edge];

                    var edgeData = edges[Math.Abs(edgeIndex)];

                    face.Points.Add(vertexes[edgeIndex > 0 ? edgeData.start : edgeData.end]);
                }

                var texInfo = EndianConverter.Little(_reader.ReadInt16());

                face.TextureInfo = textureInfos[texInfo];

                face.Styles = new byte[BSPConstants.MaxLightmaps];

                foreach (var style in Enumerable.Range(0, BSPConstants.MaxLightmaps))
                {
                    face.Styles[style] = _reader.ReadByte();
                }

                face.LightOffset = EndianConverter.Little(_reader.ReadInt32());

                //Need to rescale the offset to match an index into the Rgb24 lighting array
                if (face.LightOffset != -1)
                {
                    face.LightOffset /= 3;
                }

                CalculateExtents(face);

                faces.Add(face);
            }

            return faces;
        }

        private List<int> ReadMarkSurfaces(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<ushort>();

            var markSurfaces = new List<int>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                markSurfaces.Add(EndianConverter.Little(_reader.ReadUInt16()));
            }

            return markSurfaces;
        }

        private unsafe List<Leaf> ReadLeafs(ref Lump lump, IReadOnlyList<int> markSurfaces, IReadOnlyList<Face> faces)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Leaf>();

            var leaves = new List<Leaf>(count);

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Leaf>());

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

        private unsafe List<Model> ReadModels(ref Lump lump, IReadOnlyList<Face> faces)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var count = lump.filelen / Marshal.SizeOf<Disk.Model>();

            var models = new List<Model>();

            foreach (var i in Enumerable.Range(0, count))
            {
                var data = _reader.ReadBytes(Marshal.SizeOf<Disk.Model>());

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

                foreach (var node in Enumerable.Range(0, BSPConstants.MaxHulls))
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

        private string ReadEntities(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            var rawBytes = _reader.ReadBytes(lump.filelen);

            return StringUtils.GetStringFromNullTerminated(Encoding.UTF8, rawBytes);
        }

        private byte[] ReadVisibility(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            return _reader.ReadBytes(lump.filelen);
        }

        private Rgb24[] ReadLighting(ref Lump lump)
        {
            _reader.BaseStream.Position = lump.fileofs;

            if (lump.filelen == 0)
            {
                return Array.Empty<Rgb24>();
            }

            var data = _reader.ReadBytes(lump.filelen);

            var lighting = new Rgb24[data.Length / 3];

            for (var i = 0; i < (data.Length / 3); ++i)
            {
                lighting[i].R = data[i * 3];
                lighting[i].G = data[(i * 3) + 1];
                lighting[i].B = data[(i * 3) + 2];
            }

            return lighting;
        }

        /// <summary>
        /// Reads the BSP file
        /// </summary>
        /// <returns></returns>
        public BSPFile ReadBSPFile()
        {
            _reader.BaseStream.Position = _startPosition;

            var header = ReadHeader();

            //Determine if this is a Blue Shift BSP file
            //This works by checking if the planes lump actually contains planes
            //It can only contain planes if the length is a multiple of the plane data structure size,
            //AND the entities lump is a multiple of the size
            //These 2 lumps are switched in those BSP files
            var isBlueShiftBSP = IsBlueShiftBSP(header);

            var entitiesLumpId = isBlueShiftBSP ? LumpId.Planes : LumpId.Entities;
            var planesLumpId = isBlueShiftBSP ? LumpId.Entities : LumpId.Planes;

            var mipTextures = ReadMipTextures(ref header.Lumps[(int)LumpId.Textures]);
            var planes = ReadPlanes(ref header.Lumps[(int)planesLumpId]);

            var textureInfos = ReadTextureInfos(ref header.Lumps[(int)LumpId.TexInfo], mipTextures);

            var vertexes = ReadVertexes(ref header.Lumps[(int)LumpId.Vertexes]);
            var edges = ReadEdges(ref header.Lumps[(int)LumpId.Edges]);
            var surfEdges = ReadSurfEdges(ref header.Lumps[(int)LumpId.SurfEdges]);
            var faces = ReadFaces(ref header.Lumps[(int)LumpId.Faces], planes, vertexes, edges, surfEdges, textureInfos);

            var clipNodes = ReadClipNodes(ref header.Lumps[(int)LumpId.ClipNodes], planes);

            var markSurfaces = ReadMarkSurfaces(ref header.Lumps[(int)LumpId.MarkSurfaces]);

            var leaves = ReadLeafs(ref header.Lumps[(int)LumpId.Leafs], markSurfaces, faces);

            var nodes = ReadNodes(ref header.Lumps[(int)LumpId.Nodes], planes, faces, leaves);

            var models = ReadModels(ref header.Lumps[(int)LumpId.Models], faces);

            var entities = ReadEntities(ref header.Lumps[(int)entitiesLumpId]);

            var visibility = ReadVisibility(ref header.Lumps[(int)LumpId.Visibility]);

            var lighting = ReadLighting(ref header.Lumps[(int)LumpId.Lighting]);

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
        /// Computes the CRC32 for this BSP file
        /// The stream position is left unmodified
        /// </summary>
        /// <returns></returns>
        public uint ComputeCRC()
        {
            var currentPosition = _reader.BaseStream.Position;

            _reader.BaseStream.Position = _startPosition;

            try
            {
                var header = ReadHeader();

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

                    _reader.BaseStream.Position = header.Lumps[i].fileofs;

                    var bytesLeft = header.Lumps[i].filelen;

                    while (bytesLeft > 0)
                    {
                        var bytesToRead = bytesLeft < buffer.Length ? bytesLeft : buffer.Length;

                        var bytesRead = _reader.Read(buffer, 0, bytesToRead);

                        if (bytesRead != bytesToRead)
                        {
                            var totalRead = header.Lumps[i].filelen - bytesLeft - (bytesToRead - bytesRead);
                            throw new FileLoadFailureException($"BSP lump {i} has invalid file length data, expected {header.Lumps[i].filelen}, got {totalRead}");
                        }

                        crc = Crc32Algorithm.Append(crc, buffer, 0, bytesToRead);

                        bytesLeft -= bytesToRead;
                    }
                }

                return crc;
            }
            finally
            {
                _reader.BaseStream.Position = currentPosition;
            }
        }
    }
}
