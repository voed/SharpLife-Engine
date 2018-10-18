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

using SharpLife.Game.Shared.Physics;
using SharpLife.Models;
using SharpLife.Models.BSP.FileFormat;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.Game.Shared.Models.BSP
{
    public sealed class BSPModel : BaseModel
    {
        public BSPFile BSPFile { get; }

        public Model SubModel { get; }

        public IReadOnlyList<Hull> Hulls { get; }

        public float Radius { get; }

        public BSPModel(string name, uint crc, BSPFile bspFile, Model subModel, Hull hull0)
            : base(name, crc, subModel.Mins, subModel.Maxs)
        {
            BSPFile = bspFile ?? throw new ArgumentNullException(nameof(bspFile));
            SubModel = subModel ?? throw new ArgumentNullException(nameof(subModel));

            var hulls = new Hull[BSPConstants.MaxHulls];

            hulls[0] = new Hull(subModel.HeadNodes[0], bspFile.ClipNodes.Count - 1, hull0.ClipMins, hull0.ClipMaxs, hull0.ClipNodes, hull0.Planes);
            hulls[1] = new Hull(subModel.HeadNodes[1], bspFile.ClipNodes.Count - 1, PhysicsConstants.Hull1.ClipMins, PhysicsConstants.Hull1.ClipMaxs, hull0.ClipNodes, new Memory<SharpLife.Models.BSP.FileFormat.Plane>(BSPFile.Planes));
            hulls[2] = new Hull(subModel.HeadNodes[2], bspFile.ClipNodes.Count - 1, PhysicsConstants.Hull2.ClipMins, PhysicsConstants.Hull2.ClipMaxs, hull0.ClipNodes, new Memory<SharpLife.Models.BSP.FileFormat.Plane>(BSPFile.Planes));
            hulls[3] = new Hull(subModel.HeadNodes[3], bspFile.ClipNodes.Count - 1, PhysicsConstants.Hull3.ClipMins, PhysicsConstants.Hull3.ClipMaxs, hull0.ClipNodes, new Memory<SharpLife.Models.BSP.FileFormat.Plane>(BSPFile.Planes));

            Hulls = hulls;

            var radius = new Vector3(
                Math.Abs(subModel.Mins.X) > Math.Abs(subModel.Maxs.X) ? Math.Abs(subModel.Mins.X) : Math.Abs(subModel.Maxs.X),
                Math.Abs(subModel.Mins.Y) > Math.Abs(subModel.Maxs.Y) ? Math.Abs(subModel.Mins.Y) : Math.Abs(subModel.Maxs.Y),
                Math.Abs(subModel.Mins.Z) > Math.Abs(subModel.Maxs.Z) ? Math.Abs(subModel.Mins.Z) : Math.Abs(subModel.Maxs.Z)
                );

            Radius = radius.Length();
        }
    }
}
