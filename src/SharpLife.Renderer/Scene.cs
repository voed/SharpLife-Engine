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

using SharpLife.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer
{
    public class Scene
    {
        private readonly Octree<CullRenderable> _octree
            = new Octree<CullRenderable>(new BoundingBox(Vector3.One * -50, Vector3.One * 50), 2);

        private readonly List<Renderable> _freeRenderables = new List<Renderable>();
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();

        private readonly ConcurrentDictionary<RenderPasses, Func<CullRenderable, bool>> _filters
            = new ConcurrentDictionary<RenderPasses, Func<CullRenderable, bool>>(new RenderPassesComparer());

        public Camera Camera { get; }

        public Scene(IInputSystem inputSystem, GraphicsDevice gd, int viewWidth, int viewHeight)
        {
            Camera = new Camera(inputSystem, gd, viewWidth, viewHeight);
            _updateables.Add(Camera);
        }

        public void AddRenderable(Renderable r)
        {
            if (r is CullRenderable cr)
            {
                _octree.AddItem(cr.BoundingBox, cr);
            }
            else
            {
                _freeRenderables.Add(r);
            }
        }

        public void AddUpdateable(IUpdateable updateable)
        {
            Debug.Assert(updateable != null);
            _updateables.Add(updateable);
        }

        public void Update(float deltaSeconds)
        {
            foreach (IUpdateable updateable in _updateables)
            {
                updateable.Update(deltaSeconds);
            }
        }

        public void RenderAllStages(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            RenderAllSingleThread(gd, cl, sc);
        }

        private void RenderAllSingleThread(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            float depthClear = gd.IsDepthRangeZeroToOne ? 0f : 1f;

            // Main scene
            cl.SetFramebuffer(sc.MainSceneFramebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Grey);
            var fbWidth = sc.MainSceneFramebuffer.Width;
            var fbHeight = sc.MainSceneFramebuffer.Height;
            cl.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
            cl.SetFullViewports();
            cl.SetFullScissorRects();
            cl.ClearDepthStencil(depthClear);
            sc.UpdateCameraBuffers(cl);
            var cameraFrustum = new BoundingFrustum(Camera.ViewMatrix * Camera.ProjectionMatrix);
            Render(gd, cl, sc, RenderPasses.Standard, cameraFrustum, Camera.Position, _renderQueues[0], _cullableStage[0], _renderableStage[0], null, false);
            Render(gd, cl, sc, RenderPasses.AlphaBlend, cameraFrustum, Camera.Position, _renderQueues[0], _cullableStage[0], _renderableStage[0], null, false);
            Render(gd, cl, sc, RenderPasses.Overlay, cameraFrustum, Camera.Position, _renderQueues[0], _cullableStage[0], _renderableStage[0], null, false);

            if (sc.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
            {
                cl.ResolveTexture(sc.MainSceneColorTexture, sc.MainSceneResolvedColorTexture);
            }

            //Render to the swap chain buffer
            cl.SetFramebuffer(gd.SwapchainFramebuffer);
            fbWidth = gd.SwapchainFramebuffer.Width;
            fbHeight = gd.SwapchainFramebuffer.Height;
            cl.SetFullViewports();
            Render(gd, cl, sc, RenderPasses.SwapchainOutput, new BoundingFrustum(), Camera.Position, _renderQueues[0], _cullableStage[0], _renderableStage[0], null, false);

            cl.End();

            _resourceUpdateCL.Begin();
            foreach (Renderable renderable in _allPerFrameRenderablesSet)
            {
                renderable.UpdatePerFrameResources(gd, _resourceUpdateCL, sc);
            }
            _resourceUpdateCL.End();

            gd.SubmitCommands(_resourceUpdateCL);
            gd.SubmitCommands(cl);
        }

        public void Render(
            GraphicsDevice gd,
            CommandList rc,
            SceneContext sc,
            RenderPasses pass,
            BoundingFrustum frustum,
            Vector3 viewPosition,
            RenderQueue renderQueue,
            List<CullRenderable> cullRenderableList,
            List<Renderable> renderableList,
            Comparer<RenderItemIndex> comparer,
            bool threaded)
        {
            renderQueue.Clear();

            cullRenderableList.Clear();
            CollectVisibleObjects(ref frustum, pass, cullRenderableList);
            renderQueue.AddRange(cullRenderableList, viewPosition);

            renderableList.Clear();
            CollectFreeObjects(pass, renderableList);
            renderQueue.AddRange(renderableList, viewPosition);

            if (comparer == null)
            {
                renderQueue.Sort();
            }
            else
            {
                renderQueue.Sort(comparer);
            }

            foreach (Renderable renderable in renderQueue)
            {
                renderable.Render(gd, rc, sc, pass);
            }

            if (threaded)
            {
                lock (_allPerFrameRenderablesSet)
                {
                    foreach (CullRenderable thing in cullRenderableList) { _allPerFrameRenderablesSet.Add(thing); }
                    foreach (Renderable thing in renderableList) { _allPerFrameRenderablesSet.Add(thing); }
                }
            }
            else
            {
                foreach (CullRenderable thing in cullRenderableList) { _allPerFrameRenderablesSet.Add(thing); }
                foreach (Renderable thing in renderableList) { _allPerFrameRenderablesSet.Add(thing); }
            }
        }

        private readonly HashSet<Renderable> _allPerFrameRenderablesSet = new HashSet<Renderable>();
        private readonly RenderQueue[] _renderQueues = Enumerable.Range(0, 4).Select(_ => new RenderQueue()).ToArray();
        private readonly List<CullRenderable>[] _cullableStage = Enumerable.Range(0, 4).Select(_ => new List<CullRenderable>()).ToArray();
        private readonly List<Renderable>[] _renderableStage = Enumerable.Range(0, 4).Select(_ => new List<Renderable>()).ToArray();

        private void CollectVisibleObjects(
            ref BoundingFrustum frustum,
            RenderPasses renderPass,
            List<CullRenderable> renderables)
        {
            _octree.GetContainedObjects(frustum, renderables, GetFilter(renderPass));
        }

        private void CollectFreeObjects(RenderPasses renderPass, List<Renderable> renderables)
        {
            foreach (Renderable r in _freeRenderables)
            {
                if ((r.RenderPasses & renderPass) != 0)
                {
                    renderables.Add(r);
                }
            }
        }

        private static readonly Func<RenderPasses, Func<CullRenderable, bool>> s_createFilterFunc = CreateFilter;
        private CommandList _resourceUpdateCL;

        private Func<CullRenderable, bool> GetFilter(RenderPasses passes)
        {
            return _filters.GetOrAdd(passes, s_createFilterFunc);
        }

        private static Func<CullRenderable, bool> CreateFilter(RenderPasses rp)
        {
            // This cannot be inlined into GetFilter -- a Roslyn bug causes copious allocations.
            // https://github.com/dotnet/roslyn/issues/22589
            return cr => (cr.RenderPasses & rp) == rp;
        }

        internal void DestroyAllDeviceObjects()
        {
            _cullableStage[0].Clear();
            _octree.GetAllContainedObjects(_cullableStage[0]);
            foreach (CullRenderable cr in _cullableStage[0])
            {
                cr.DestroyDeviceObjects();
            }
            foreach (Renderable r in _freeRenderables)
            {
                r.DestroyDeviceObjects();
            }

            _resourceUpdateCL.Dispose();
        }

        public void CreateAllDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            _cullableStage[0].Clear();
            _octree.GetAllContainedObjects(_cullableStage[0]);
            foreach (CullRenderable cr in _cullableStage[0])
            {
                cr.CreateDeviceObjects(gd, cl, sc);
            }
            foreach (Renderable r in _freeRenderables)
            {
                r.CreateDeviceObjects(gd, cl, sc);
            }

            _resourceUpdateCL = gd.ResourceFactory.CreateCommandList();
            _resourceUpdateCL.Name = "Scene Resource Update Command List";
        }

        private class RenderPassesComparer : IEqualityComparer<RenderPasses>
        {
            public bool Equals(RenderPasses x, RenderPasses y) => x == y;
            public int GetHashCode(RenderPasses obj) => ((byte)obj).GetHashCode();
        }
    }
}
