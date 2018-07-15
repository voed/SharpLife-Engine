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

using System;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer
{
    public abstract class Renderable : IDisposable
    {
        public abstract void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc);
        public abstract void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass);
        public abstract void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc);
        public abstract void DestroyDeviceObjects();
        public abstract RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
        public virtual RenderPasses RenderPasses => RenderPasses.Standard;

        public void Dispose()
        {
            DestroyDeviceObjects();
        }
    }

    public abstract class CullRenderable : Renderable
    {
        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(BoundingBox) == ContainmentType.Disjoint;
        }

        public abstract BoundingBox BoundingBox { get; }
    }
}