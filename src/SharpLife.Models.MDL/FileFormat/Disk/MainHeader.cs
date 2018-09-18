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

using System.Numerics;

namespace SharpLife.Models.MDL.FileFormat.Disk
{
    internal unsafe struct MainHeader
    {
        internal int Id;

        internal int Version;

        internal fixed byte Name[64];

        internal int Length;

        internal Vector3 EyePosition;

        internal Vector3 Min;
        internal Vector3 Max;

        internal Vector3 BBMin;
        internal Vector3 BBMax;

        internal int Flags;

        internal int NumBones;
        internal int BoneIndex;

        internal int NumBoneControllers;
        internal int BoneControllerIndex;

        internal int NumHitboxes;
        internal int HitboxIndex;

        internal int NumSeq;
        internal int SeqIndex;

        internal int NumSeqGroups;
        internal int SeqGroupIndex;

        internal int NumTextures;
        internal int TextureIndex;
        internal int TextureDataIndex;

        internal int NumSkinRef;
        internal int NumSkinFamilies;
        internal int SkinIndex;

        internal int NumBodyParts;
        internal int BodyPartIndex;

        internal int NumAttachments;
        internal int AttachmentIndex;

        internal int SoundTable;
        internal int SoundIndex;
        internal int SoundGroups;
        internal int SoundGroupIndex;

        internal int NumTransitions;
        internal int TransitionIndex;
    }
}
