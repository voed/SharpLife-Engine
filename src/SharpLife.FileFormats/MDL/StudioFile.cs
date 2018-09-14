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

using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.FileFormats.MDL
{
    //TODO: implement
    public class StudioFile
    {
        public MDLVersion Version { get; set; }

        public Vector3 EyePosition { get; set; }

        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Vector3 BBMin { get; set; }
        public Vector3 BBMax { get; set; }

        public MDLFlags Flags { get; set; }

        public List<Bone> Bones { get; set; }

        public List<BoneController> BoneControllers { get; set; }

        public List<BoundingBox> Hitboxes { get; set; }

        public List<SequenceDescriptor> Sequences { get; set; }

        public List<SequenceGroup> SequenceGroups { get; set; }

        public List<Texture> Textures { get; set; }

        public List<List<int>> Skins { get; set; }

        public List<BodyPart> BodyParts { get; set; }

        public List<Attachment> Attachments { get; set; }

        public List<List<byte>> Transitions { get; set; }
    }
}
