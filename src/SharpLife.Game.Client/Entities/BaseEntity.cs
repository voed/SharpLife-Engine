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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Renderer.Models;
using System.Numerics;

namespace SharpLife.Game.Client.Entities
{
    [Networkable]
    public abstract class BaseEntity : SharedBaseEntity, IRenderableEntity
    {
        protected BaseEntity(bool networked)
            : base(networked)
        {
        }

        public virtual void Render(IModelRenderer modelRenderer)
        {
            if (Model != null)
            {
                var renderData = new ModelRenderData { Model = Model, Origin = Origin, Angles = Angles, Scale = new Vector3(1, 1, 1) };

                modelRenderer.Render(ref renderData);
            }
        }
    }
}
