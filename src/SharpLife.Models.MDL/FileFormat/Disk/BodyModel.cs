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

namespace SharpLife.Models.MDL.FileFormat.Disk
{
    internal unsafe struct BodyModel
    {
        internal const int NameSize = 64;

        internal fixed byte Name[NameSize];

#pragma warning disable CS0649
        internal int Type;

        internal float BoundingRadius;

        internal int NumMesh;
        internal int MeshIndex;

        internal int NumVerts;       // number of unique vertices
        internal int VertInfoIndex;  // vertex bone info
        internal int VertIndex;      // vertex vec3_t
        internal int NumNorms;       // number of unique surface normals
        internal int NormInfoIndex;  // normal bone info
        internal int NormIndex;      // normal vec3_t

        internal int NumGroups;      // deformation groups
        internal int GroupIndex;
#pragma warning restore CS0649
    }
}
