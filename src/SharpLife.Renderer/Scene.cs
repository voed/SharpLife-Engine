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
using SharpLife.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer
{
    public class Scene : IViewState
    {
        private readonly List<IResourceContainer> _resourceContainers = new List<IResourceContainer>();
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();
        private readonly List<IRenderable> _renderables = new List<IRenderable>();

        public Camera Camera { get; }

        public Vector3 Origin => Camera.Position;

        public Vector3 Angles => VectorUtils.VectorToAngles(Vector3.Transform(Camera.DefaultLookDirection, Camera.RotationMatrix));

        private DirectionalVectors _viewAngles;

        public DirectionalVectors ViewVectors => _viewAngles;

        public Scene(IInputSystem inputSystem, GraphicsDevice gd, int viewWidth, int viewHeight)
        {
            Camera = new Camera(inputSystem, gd, viewWidth, viewHeight);
            _updateables.Add(Camera);
        }

        public void AddContainer(IResourceContainer r)
        {
            _resourceContainers.Add(r);
        }

        public void RemoveContainer(IResourceContainer r)
        {
            _resourceContainers.Remove(r);
        }

        public void AddRenderable(IRenderable r)
        {
            _renderables.Add(r);
        }

        public void RemoveRenderable(IRenderable r)
        {
            _renderables.Remove(r);
        }

        public void AddUpdateable(IUpdateable updateable)
        {
            Debug.Assert(updateable != null);
            _updateables.Add(updateable);
        }

        public void Update(float deltaSeconds)
        {
            VectorUtils.AngleToVectors(Angles, out _viewAngles);

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
            Render(gd, cl, sc, RenderPasses.Standard, cameraFrustum, Camera.Position, _renderQueues[0], _renderableStage[0], null, false);
            Render(gd, cl, sc, RenderPasses.AlphaBlend, cameraFrustum, Camera.Position, _renderQueues[0], _renderableStage[0], null, false);
            Render(gd, cl, sc, RenderPasses.Overlay, cameraFrustum, Camera.Position, _renderQueues[0], _renderableStage[0], null, false);

            if (sc.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
            {
                cl.ResolveTexture(sc.MainSceneColorTexture, sc.MainSceneResolvedColorTexture);
            }

            //Render to the swap chain buffer
            cl.SetFramebuffer(gd.SwapchainFramebuffer);
            fbWidth = gd.SwapchainFramebuffer.Width;
            fbHeight = gd.SwapchainFramebuffer.Height;
            cl.SetFullViewports();
            Render(gd, cl, sc, RenderPasses.SwapchainOutput, new BoundingFrustum(), Camera.Position, _renderQueues[0], _renderableStage[0], null, false);

            cl.End();

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
            List<IRenderable> renderableList,
            Comparer<RenderItemIndex> comparer,
            bool threaded)
        {
            renderQueue.Clear();

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

            foreach (var renderable in renderQueue)
            {
                renderable.Render(gd, rc, sc, pass);
            }
        }

        private readonly RenderQueue[] _renderQueues = Enumerable.Range(0, 4).Select(_ => new RenderQueue()).ToArray();
        private readonly List<IRenderable>[] _renderableStage = Enumerable.Range(0, 4).Select(_ => new List<IRenderable>()).ToArray();

        private void CollectFreeObjects(RenderPasses renderPass, List<IRenderable> renderables)
        {
            foreach (var r in _renderables)
            {
                if ((r.RenderPasses & renderPass) != 0)
                {
                    renderables.Add(r);
                }
            }
        }

        public void DestroyAllDeviceObjects(ResourceScope scope)
        {
            foreach (var r in _resourceContainers)
            {
                r.DestroyDeviceObjects(scope);
            }
        }

        public void CreateAllDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            foreach (var r in _resourceContainers)
            {
                r.CreateDeviceObjects(gd, cl, sc, scope);
            }
        }

        private class RenderPassesComparer : IEqualityComparer<RenderPasses>
        {
            public bool Equals(RenderPasses x, RenderPasses y) => x == y;
            public int GetHashCode(RenderPasses obj) => ((byte)obj).GetHashCode();
        }
    }
}
