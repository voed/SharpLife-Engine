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

namespace SharpLife.Models.BSP.FileFormat.Disk
{
    internal enum LumpId
    {
        FirstLump = 0,

        Entities = FirstLump,
        Planes = 1,
        Textures = 2,
        Vertexes = 3,
        Visibility = 4,
        Nodes = 5,
        TexInfo = 6,
        Faces = 7,
        Lighting = 8,
        ClipNodes = 9,
        Leafs = 10,
        MarkSurfaces = 11,
        Edges = 12,
        SurfEdges = 13,
        Models = 14,

        LastLump = Models
    }
}
