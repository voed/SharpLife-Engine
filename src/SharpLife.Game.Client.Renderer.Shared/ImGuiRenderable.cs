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
using SharpLife.Renderer;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace SharpLife.Game.Client.Renderer.Shared
{
    public class ImGuiRenderable : ResourceContainer, IUpdateable, IRenderable
    {
        private IInputSystem _inputSystem;
        private ImGuiRenderer _imguiRenderer;
        private int _width;
        private int _height;

        public ImGuiRenderable(IInputSystem inputSystem, int width, int height)
        {
            _inputSystem = inputSystem;
            _width = width;
            _height = height;
        }

        public void WindowResized(int width, int height) => _imguiRenderer.WindowResized(width, height);

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            if ((scope & ResourceScope.Global) == 0)
            {
                return;
            }

            if (_imguiRenderer == null)
            {
                _imguiRenderer = new ImGuiRenderer(gd, sc.MainSceneFramebuffer.OutputDescription, _width, _height);
            }
            else
            {
                _imguiRenderer.CreateDeviceResources(gd, sc.MainSceneFramebuffer.OutputDescription);
            }
        }

        public override void DestroyDeviceObjects(ResourceScope scope)
        {
            if ((scope & ResourceScope.Global) == 0)
            {
                return;
            }

            _imguiRenderer.Dispose();
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            Debug.Assert(renderPass == RenderPasses.Overlay);
            _imguiRenderer.Render(gd, cl);
        }

        public RenderPasses RenderPasses => RenderPasses.Overlay;

        public void Update(float deltaSeconds)
        {
            _imguiRenderer.Update(deltaSeconds, _inputSystem.Snapshot);
        }
    }
}
