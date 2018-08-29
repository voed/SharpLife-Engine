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

using SharpLife.FileFormats.BSP;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SharpLife.Engine.Shared.Models.BSP
{
    public sealed class BSPModel : BaseModel
    {
        public BSPFile BSPFile { get; }

        public Model SubModel { get; }

        public IReadOnlyList<Hull> Hulls { get; }

        public float Radius { get; }

        public BSPModel(string name, uint crc, BSPFile bspFile, Model subModel)
            : base(name, crc)
        {
            BSPFile = bspFile ?? throw new ArgumentNullException(nameof(bspFile));
            SubModel = subModel ?? throw new ArgumentNullException(nameof(subModel));

            var hulls = new Hull[BSPConstants.MaxHulls];

            for (var i = 0; i < BSPConstants.MaxHulls; ++i)
            {
                hulls[i] = new Hull(subModel.HeadNodes[i], bspFile.ClipNodes.Count - 1);
            }

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
