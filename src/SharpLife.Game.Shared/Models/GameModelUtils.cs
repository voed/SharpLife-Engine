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

using SharpLife.Engine.Shared;
using SharpLife.Models;
using SharpLife.Models.BSP.Loading;
using SharpLife.Models.MDL.Loading;
using SharpLife.Models.SPR.Loading;
using System.Collections.Generic;

namespace SharpLife.Game.Shared.Models
{
    public static class GameModelUtils
    {
        public static IReadOnlyList<IModelLoader> GetModelLoaders()
        {
            return new List<IModelLoader>
            {
                new SpriteModelLoader(),
                new StudioModelLoader(),

                //BSP loader comes last due to not having a way to positively recognize the format
                new BSPModelLoader(Framework.BSPModelNamePrefix)
            };
        }
    }
}
