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

using SharpLife.Models.MDL.FileFormat.Disk;
using SharpLife.Renderer.Utility;
using SharpLife.Utility;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.Models.MDL.FileFormat
{
    public sealed class StudioLoader
    {
        private readonly BinaryReader _reader;

        private readonly long _startPosition;

        public StudioLoader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _startPosition = _reader.BaseStream.Position;
        }

        public StudioLoader(Stream stream, bool leaveOpen)
            : this(new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, leaveOpen))
        {
        }

        public StudioLoader(Stream stream)
            : this(stream, false)
        {
        }

        public StudioLoader(string fileName)
            : this(File.OpenRead(fileName))
        {
        }

        private static int ReadIdentifier(BinaryReader reader)
        {
            return EndianConverter.Little(reader.ReadInt32());
        }

        public static bool IsStudioFile(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var originalPosition = reader.BaseStream.Position;

            try
            {
                return MDLConstants.MainHeaderIdentifier == ReadIdentifier(reader);
            }
            finally
            {
                reader.BaseStream.Position = originalPosition;
            }
        }

        private MainHeader ReadHeader()
        {
            var position = _reader.BaseStream.Position;

            var identifier = ReadIdentifier(_reader);

            //Verify that we can load this MDL file
            //TODO: maybe pass the invalid value along with the exception
            if (identifier != MDLConstants.MainHeaderIdentifier)
            {
                throw new InvalidMDLIdException();
            }

            var version = EndianConverter.Little(_reader.ReadInt32());

            if (!Enum.IsDefined(typeof(MDLVersion), version))
            {
                throw new InvalidMDLVersionException();
            }

            _reader.BaseStream.Position = position;

            var header = _reader.ReadStructure<MainHeader>();

            header.Id = EndianConverter.Little(header.Id);
            header.Version = EndianConverter.Little(header.Version);
            header.Length = EndianConverter.Little(header.Length);

            header.EyePosition = EndianTypeConverter.Little(header.EyePosition);

            header.Min = EndianTypeConverter.Little(header.Min);
            header.Max = EndianTypeConverter.Little(header.Max);

            header.BBMin = EndianTypeConverter.Little(header.BBMin);
            header.BBMax = EndianTypeConverter.Little(header.BBMax);

            header.Flags = EndianConverter.Little(header.Flags);

            header.NumBones = EndianConverter.Little(header.NumBones);
            header.BoneIndex = EndianConverter.Little(header.BoneIndex);

            header.NumBoneControllers = EndianConverter.Little(header.NumBoneControllers);
            header.BoneControllerIndex = EndianConverter.Little(header.BoneControllerIndex);

            header.NumHitboxes = EndianConverter.Little(header.NumHitboxes);
            header.HitboxIndex = EndianConverter.Little(header.HitboxIndex);

            header.NumSeq = EndianConverter.Little(header.NumSeq);
            header.SeqIndex = EndianConverter.Little(header.SeqIndex);

            header.NumSeqGroups = EndianConverter.Little(header.NumSeqGroups);
            header.SeqGroupIndex = EndianConverter.Little(header.SeqGroupIndex);

            header.NumTextures = EndianConverter.Little(header.NumTextures);
            header.TextureIndex = EndianConverter.Little(header.TextureIndex);
            header.TextureDataIndex = EndianConverter.Little(header.TextureDataIndex);

            header.NumSkinRef = EndianConverter.Little(header.NumSkinRef);
            header.NumSkinFamilies = EndianConverter.Little(header.NumSkinFamilies);
            header.SkinIndex = EndianConverter.Little(header.SkinIndex);

            header.NumBodyParts = EndianConverter.Little(header.NumBodyParts);
            header.BodyPartIndex = EndianConverter.Little(header.BodyPartIndex);

            header.NumAttachments = EndianConverter.Little(header.NumAttachments);
            header.AttachmentIndex = EndianConverter.Little(header.AttachmentIndex);

            header.SoundTable = EndianConverter.Little(header.SoundTable);
            header.SoundIndex = EndianConverter.Little(header.SoundIndex);
            header.SoundGroups = EndianConverter.Little(header.SoundGroups);
            header.SoundGroupIndex = EndianConverter.Little(header.SoundGroupIndex);

            header.NumTransitions = EndianConverter.Little(header.NumTransitions);
            header.TransitionIndex = EndianConverter.Little(header.TransitionIndex);

            return header;
        }

        private unsafe List<Bone> ReadBones(in MainHeader header)
        {
            _reader.BaseStream.Position = header.BoneIndex;

            var list = new List<Bone>(header.NumBones);

            for (var i = 0; i < header.NumBones; ++i)
            {
                var rawBone = _reader.ReadStructure<Disk.Bone>();

                var bone = new Bone
                {
                    Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawBone.Name, Disk.Bone.NameSize)),
                    Parent = EndianConverter.Little(rawBone.Parent)
                };

                for (var j = 0; j < MDLConstants.NumAxes; ++j)
                {
                    bone.Values[j] = EndianConverter.Little(rawBone.Value[j]);
                    bone.Scales[j] = EndianConverter.Little(rawBone.Scale[j]);
                    bone.BoneControllers[j] = MDLConstants.NoBoneController;
                }

                //Controllers are initialized after the list is loaded

                list.Add(bone);
            }

            return list;
        }

        private List<BoneController> ReadBoneControllers(in MainHeader header, IReadOnlyList<Bone> bones)
        {
            _reader.BaseStream.Position = header.BoneControllerIndex;

            var list = new List<BoneController>(header.NumBoneControllers);

            for (var i = 0; i < header.NumBoneControllers; ++i)
            {
                var rawBoneController = _reader.ReadStructure<Disk.BoneController>();

                var boneController = new BoneController
                {
                    Bone = bones[EndianConverter.Little(rawBoneController.Bone)],
                    Type = (MotionTypes)EndianConverter.Little(rawBoneController.Type),
                    Start = EndianConverter.Little(rawBoneController.Start),
                    End = EndianConverter.Little(rawBoneController.End),
                    Rest = EndianConverter.Little(rawBoneController.Rest),
                    Index = EndianConverter.Little(rawBoneController.Index)
                };

                //Determine which controller it is
                int controllerIndex;

                switch (boneController.Type)
                {
                    case MotionTypes.X:
                        controllerIndex = 0;
                        break;
                    case MotionTypes.Y:
                        controllerIndex = 1;
                        break;
                    case MotionTypes.Z:
                        controllerIndex = 2;
                        break;
                    case MotionTypes.XR:
                        controllerIndex = 3;
                        break;
                    case MotionTypes.YR:
                        controllerIndex = 4;
                        break;
                    case MotionTypes.ZR:
                        controllerIndex = 5;
                        break;

                    default: throw new FileLoadFailureException($"Invalid bone controller type {boneController.Type}");
                }

                boneController.Bone.BoneControllers[controllerIndex] = i;

                list.Add(boneController);
            }

            return list;
        }

        private List<BoundingBox> ReadHitBoxes(in MainHeader header, IReadOnlyList<Bone> bones)
        {
            _reader.BaseStream.Position = header.HitboxIndex;

            var list = new List<BoundingBox>(header.NumHitboxes);

            for (var i = 0; i < header.NumHitboxes; ++i)
            {
                var rawHitbox = _reader.ReadStructure<Disk.BoundingBox>();

                list.Add(new BoundingBox
                {
                    Bone = bones[EndianConverter.Little(rawHitbox.Bone)],
                    Min = EndianTypeConverter.Little(rawHitbox.BBMin),
                    Max = EndianTypeConverter.Little(rawHitbox.BBMax)
                });
            }

            return list;
        }

        private unsafe List<Event> ReadEvents(int eventIndex, int numEvents)
        {
            _reader.BaseStream.Position = eventIndex;

            var list = new List<Event>(numEvents);

            for (var i = 0; i < numEvents; ++i)
            {
                var rawEvent = _reader.ReadStructure<Disk.Event>();

                list.Add(new Event
                {
                    Frame = rawEvent.Frame,
                    EventId = rawEvent.EventId,
                    Type = rawEvent.Type,
                    Options = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawEvent.Options, Disk.Event.OptionsSize))
                });
            }

            return list;
        }

        private unsafe (List<SequenceDescriptor>, List<Disk.SequenceDescriptor>) ReadSequences(in MainHeader header)
        {
            var sequenceSize = Marshal.SizeOf<Disk.SequenceDescriptor>();

            var list = new List<SequenceDescriptor>(header.NumSeq);

            var rawSequences = new List<Disk.SequenceDescriptor>(header.NumSeq);

            for (var i = 0; i < header.NumSeq; ++i)
            {
                //Reset the position because ReadEvents changes it
                _reader.BaseStream.Position = header.SeqIndex + (sequenceSize * i);

                var rawSequence = _reader.ReadStructure<Disk.SequenceDescriptor>();

                //Convert these so ReadAnimationBlends can use them
                rawSequence.NumBlends = EndianConverter.Little(rawSequence.NumBlends);
                rawSequence.AnimIndex = EndianConverter.Little(rawSequence.AnimIndex);
                rawSequence.SeqGroup = EndianConverter.Little(rawSequence.SeqGroup);

                rawSequences.Add(rawSequence);

                //Part of sequence conversion is done when reading sequence groups
                var sequence = new SequenceDescriptor
                {
                    Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawSequence.Label, Disk.SequenceDescriptor.LabelSize)),

                    FPS = EndianConverter.Little(rawSequence.FPS),

                    Flags = (SequenceFlags)EndianConverter.Little(rawSequence.Flags),

                    Activity = EndianConverter.Little(rawSequence.Activity),
                    ActivityWeight = EndianConverter.Little(rawSequence.ActivityWeight),

                    Events = ReadEvents(EndianConverter.Little(rawSequence.EventIndex), EndianConverter.Little(rawSequence.NumEvents)),

                    FrameCount = EndianConverter.Little(rawSequence.FrameCount),

                    MotionType = (MotionTypes)EndianConverter.Little(rawSequence.MotionType),
                    MotionBone = EndianConverter.Little(rawSequence.MotionBone),

                    LinearMovement = EndianTypeConverter.Little(rawSequence.LinearMovement),

                    BBMin = EndianTypeConverter.Little(rawSequence.BBMin),
                    BBMax = EndianTypeConverter.Little(rawSequence.BBMax),

                    EntryNode = EndianConverter.Little(rawSequence.EntryNode),
                    ExitNode = EndianConverter.Little(rawSequence.ExitNode),
                    NodeFlags = (TransitionNodeFlags)EndianConverter.Little(rawSequence.NodeFlags),
                };

                for (var blend = 0; blend < MDLConstants.NumBlendTypes; ++blend)
                {
                    ref var blendType = ref sequence.Blends[blend];

                    blendType.Type = (MotionTypes)EndianConverter.Little(rawSequence.BlendType[blend]);
                    blendType.Start = EndianConverter.Little(rawSequence.BlendStart[blend]);
                    blendType.End = EndianConverter.Little(rawSequence.BlendEnd[blend]);
                }

                list.Add(sequence);
            }

            return (list, rawSequences);
        }

        private unsafe List<SequenceGroup> ReadSequenceGroups(in MainHeader header, IReadOnlyList<SequenceDescriptor> sequences, IReadOnlyList<Disk.SequenceDescriptor> rawSequences)
        {
            var list = new List<SequenceGroup>(header.NumSeqGroups);

            for (var i = 0; i < header.NumSeqGroups; ++i)
            {
                _reader.BaseStream.Position = header.SeqGroupIndex + (Marshal.SizeOf<Disk.SequenceGroup>() * i);

                var rawGroup = _reader.ReadStructure<Disk.SequenceGroup>();

                //Reconstruct the list of sequences
                var groupSequences = new List<SequenceDescriptor>();

                for (var j = 0; j < sequences.Count; ++j)
                {
                    if (EndianConverter.Little(rawSequences[j].SeqGroup) == i)
                    {
                        groupSequences.Add(sequences[j]);

                        //The first group is embedded in the main file
                        if (i == 0)
                        {
                            sequences[j].AnimationBlends = StudioIOUtils.ReadAnimationBlends(
                                _reader,
                                header.NumBones,
                                EndianConverter.Little(rawGroup.Data),
                                rawSequences[j],
                                sequences[j]);
                        }
                    }
                }

                list.Add(new SequenceGroup
                {
                    Label = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawGroup.Label, Disk.SequenceGroup.LabelSize)),
                    Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawGroup.Name, Disk.SequenceGroup.NameSize)),
                    Sequences = groupSequences
                });
            }

            return list;
        }

        private unsafe List<Texture> ReadTextures(in MainHeader header)
        {
            var list = new List<Texture>(header.NumTextures);

            for (var i = 0; i < header.NumTextures; ++i)
            {
                _reader.BaseStream.Position = header.TextureIndex + (Marshal.SizeOf<Disk.Texture>() * i);

                var rawTexture = _reader.ReadStructure<Disk.Texture>();

                var texture = new Texture
                {
                    Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawTexture.Name, Disk.Texture.NameSize)),
                    Flags = (TextureFlags)EndianConverter.Little(rawTexture.Flags),
                    Width = EndianConverter.Little(rawTexture.Width),
                    Height = EndianConverter.Little(rawTexture.Height)
                };

                _reader.BaseStream.Position = EndianConverter.Little(rawTexture.Index);

                //Texture data is pixels followed by palette
                texture.Pixels = _reader.ReadBytes(texture.Width * texture.Height);

                //TODO: this is duplicated from WADLoader
                foreach (var j in Enumerable.Range(0, IndexPaletteConstants.NumPaletteColors))
                {
                    var paletteData = _reader.ReadBytes(IndexPaletteConstants.NumPaletteComponents * IndexPaletteConstants.PaletteComponentSizeInBytes);

                    var paletteHandle = GCHandle.Alloc(paletteData, GCHandleType.Pinned);
                    texture.Palette[j] = Marshal.PtrToStructure<Rgb24>(paletteHandle.AddrOfPinnedObject());
                    paletteHandle.Free();
                }

                list.Add(texture);
            }

            return list;
        }

        private List<List<int>> ReadSkins(in MainHeader header)
        {
            _reader.BaseStream.Position = header.SkinIndex;

            var list = new List<List<int>>(header.NumSkinFamilies);

            for (var i = 0; i < header.NumSkinFamilies; ++i)
            {
                var skinRefs = new List<int>(header.NumSkinRef);

                for (var j = 0; j < header.NumSkinRef; ++j)
                {
                    skinRefs.Add(EndianConverter.Little(_reader.ReadInt16()));
                }

                list.Add(skinRefs);
            }

            return list;
        }

        private unsafe List<BodyPart> ReadBodyParts(in MainHeader header)
        {
            var list = new List<BodyPart>(header.NumBodyParts);

            for (var i = 0; i < header.NumBodyParts; ++i)
            {
                _reader.BaseStream.Position = header.BodyPartIndex + (Marshal.SizeOf<Disk.BodyPart>() * i);

                var rawBodyPart = _reader.ReadStructure<Disk.BodyPart>();

                rawBodyPart.NumModels = EndianConverter.Little(rawBodyPart.NumModels);
                rawBodyPart.ModelIndex = EndianConverter.Little(rawBodyPart.ModelIndex);

                var models = new List<BodyModel>(rawBodyPart.NumModels);

                for (var model = 0; model < rawBodyPart.NumModels; ++model)
                {
                    _reader.BaseStream.Position = rawBodyPart.ModelIndex + (Marshal.SizeOf<Disk.BodyModel>() * model);

                    var rawBodyModel = _reader.ReadStructure<Disk.BodyModel>();

                    rawBodyModel.VertIndex = EndianConverter.Little(rawBodyModel.VertIndex);
                    rawBodyModel.VertInfoIndex = EndianConverter.Little(rawBodyModel.VertInfoIndex);
                    rawBodyModel.NumVerts = EndianConverter.Little(rawBodyModel.NumVerts);

                    rawBodyModel.NormIndex = EndianConverter.Little(rawBodyModel.NormIndex);
                    rawBodyModel.NormInfoIndex = EndianConverter.Little(rawBodyModel.NormInfoIndex);
                    rawBodyModel.NumNorms = EndianConverter.Little(rawBodyModel.NumNorms);

                    _reader.BaseStream.Position = rawBodyModel.VertIndex;

                    var vertexData = _reader.ReadStructureArray<Vector3>(rawBodyModel.NumVerts);

                    _reader.BaseStream.Position = rawBodyModel.VertInfoIndex;

                    var vertexBones = _reader.ReadStructureArray<byte>(rawBodyModel.NumVerts);

                    var vertices = new List<BodyModel.VertexInfo>(rawBodyModel.NumVerts);

                    for (var vertex = 0; vertex < rawBodyModel.NumVerts; ++vertex)
                    {
                        vertices.Add(new BodyModel.VertexInfo
                        {
                            Bone = vertexBones[vertex],
                            Vertex = vertexData[vertex]
                        });
                    }

                    var meshes = new List<BodyMesh>(rawBodyModel.NumMesh);

                    var normalOffset = 0;

                    for (var mesh = 0; mesh < rawBodyModel.NumMesh; ++mesh)
                    {
                        _reader.BaseStream.Position = rawBodyModel.MeshIndex + (Marshal.SizeOf<Disk.BodyMesh>() * mesh);

                        var rawMesh = _reader.ReadStructure<Disk.BodyMesh>();

                        rawMesh.TriIndex = EndianConverter.Little(rawMesh.TriIndex);
                        rawMesh.NumTris = EndianConverter.Little(rawMesh.NumTris);
                        rawMesh.NumNorms = EndianConverter.Little(rawMesh.NumNorms);

                        //The triangle command list has no fixed size and cannot be determined from other fields
                        //The number of triangles can be used to determine the minimum number of values that will be stored
                        //4 commands per vertex minimum
                        var triangleCommands = new List<short>(rawMesh.NumTris * 4);

                        _reader.BaseStream.Position = rawMesh.TriIndex;

                        for (var command = EndianConverter.Little(_reader.ReadInt16());
                            command != 0;
                            command = EndianConverter.Little(_reader.ReadInt16()))
                        {
                            triangleCommands.Add(command);

                            var numCommands = Math.Abs(command);

                            while (numCommands > 0)
                            {
                                --numCommands;

                                for (var cmd = 0; cmd < 4; ++cmd)
                                {
                                    triangleCommands.Add(EndianConverter.Little(_reader.ReadInt16()));
                                }
                            }
                        }

                        //TODO: Could be omitted since we know the total number of commands
                        triangleCommands.Add(0);

                        _reader.BaseStream.Position = rawBodyModel.NormIndex + normalOffset;

                        var normalData = _reader.ReadStructureArray<Vector3>(rawMesh.NumNorms);

                        _reader.BaseStream.Position = rawBodyModel.NormInfoIndex + normalOffset;

                        var normalBones = _reader.ReadStructureArray<byte>(rawMesh.NumNorms);

                        var normals = new List<BodyMesh.NormalInfo>(rawMesh.NumNorms);

                        for (var normal = 0; normal < rawMesh.NumNorms; ++normal)
                        {
                            normals.Add(new BodyMesh.NormalInfo
                            {
                                Bone = normalBones[normal],
                                Normal = normalData[normal]
                            });
                        }

                        meshes.Add(new BodyMesh
                        {
                            TriangleCommands = triangleCommands,
                            Normals = normals,
                            Skin = EndianConverter.Little(rawMesh.SkinRef)
                        });

                        normalOffset += rawMesh.NumNorms;
                    }

                    models.Add(new BodyModel
                    {
                        Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawBodyModel.Name, Disk.BodyModel.NameSize)),
                        Vertices = vertices,
                        Meshes = meshes
                    });
                }

                list.Add(new BodyPart
                {
                    Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawBodyPart.Name, Disk.BodyPart.NameSize)),
                    Base = EndianConverter.Little(rawBodyPart.BaseIndex),
                    Models = models
                });
            }

            return list;
        }

        private unsafe List<Attachment> ReadAttachments(in MainHeader header, IReadOnlyList<Bone> bones)
        {
            _reader.BaseStream.Position = header.AttachmentIndex;

            var list = new List<Attachment>(header.NumAttachments);

            for (var i = 0; i < header.NumAttachments; ++i)
            {
                var rawAttachment = _reader.ReadStructure<Disk.Attachment>();

                list.Add(new Attachment
                {
                    Name = StringUtils.GetStringFromNullTerminated(Encoding.UTF8, new ReadOnlySpan<byte>(rawAttachment.Name, Disk.Attachment.NameSize)),
                    Type = EndianConverter.Little(rawAttachment.Type),
                    Bone = bones[EndianConverter.Little(rawAttachment.Bone)],

                    Origin = EndianTypeConverter.Little(rawAttachment.Origin),

                    Vector0 = EndianTypeConverter.Little(rawAttachment.Vector0),
                    Vector1 = EndianTypeConverter.Little(rawAttachment.Vector1),
                    Vector2 = EndianTypeConverter.Little(rawAttachment.Vector2),
                });
            }

            return list;
        }

        private List<List<byte>> ReadTransitions(in MainHeader header)
        {
            _reader.BaseStream.Position = header.TransitionIndex;

            var list = new List<List<byte>>(header.NumTransitions);

            for (var i = 0; i < header.NumTransitions; ++i)
            {
                var mappings = new List<byte>(header.NumTransitions);

                for (var j = 0; j < header.NumTransitions; ++j)
                {
                    var value = _reader.ReadByte();

                    mappings.Add(value);
                }

                list.Add(mappings);
            }

            return list;
        }

        public (StudioFile, IReadOnlyList<Disk.SequenceDescriptor>) ReadStudioFile()
        {
            var header = ReadHeader();

            var studioFile = new StudioFile
            {
                Version = (MDLVersion)header.Version,
                EyePosition = header.EyePosition,
                Min = header.Min,
                Max = header.Max,
                BBMin = header.BBMin,
                BBMax = header.BBMax,
                Flags = (MDLFlags)header.Flags
            };

            IReadOnlyList<Disk.SequenceDescriptor> rawSequences = null;

            //The main file contains this data
            if (header.BoneIndex != 0)
            {
                studioFile.Bones = ReadBones(header);
                studioFile.BoneControllers = ReadBoneControllers(header, studioFile.Bones);
                studioFile.Hitboxes = ReadHitBoxes(header, studioFile.Bones);
                (studioFile.Sequences, rawSequences) = ReadSequences(header);
                studioFile.SequenceGroups = ReadSequenceGroups(header, studioFile.Sequences, rawSequences);
                studioFile.BodyParts = ReadBodyParts(header);
                studioFile.Attachments = ReadAttachments(header, studioFile.Bones);
                studioFile.Transitions = ReadTransitions(header);
            }

            //The texture file or the main file contains this data
            if (header.TextureIndex != 0)
            {
                studioFile.Textures = ReadTextures(header);
                studioFile.Skins = ReadSkins(header);
            }

            return (studioFile, rawSequences);
        }

        public uint ComputeCRC()
        {
            return CrcUtils.ComputeCRC(_reader, _startPosition);
        }
    }
}
