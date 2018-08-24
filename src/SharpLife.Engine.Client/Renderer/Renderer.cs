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

using SDL2;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.API.Engine.Client;
using SharpLife.Engine.Shared.Models;
using SharpLife.FileSystem;
using SharpLife.Input;
using SharpLife.Renderer;
using SharpLife.Renderer.BSP;
using SharpLife.Renderer.Objects;
using SharpLife.Utility;
using System;
using System.Numerics;
using Veldrid;

namespace SharpLife.Engine.Client.Renderer
{
    /// <summary>
    /// The main renderer
    /// Manages current graphics state, devices, etc
    /// </summary>
    public class Renderer : IViewState
    {
        private readonly IntPtr _window;
        private readonly IntPtr _glContext;
        private readonly GraphicsDevice _gd;
        private readonly Scene _scene;

        private readonly IFileSystem _fileSystem;

        private readonly string _envMapDirectory;

        private readonly SceneContext _sc;

        private readonly ImGuiRenderable _imGuiRenderable;

        private BSPWorldRenderable _bspWorldRenderable;

        private Skybox2D _skyboxRenderable;

        private readonly CommandList _frameCommands;

        private bool _windowResized = false;

        private event Action<int, int> _resizeHandled;

        public Scene Scene => _scene;

        public Vector3 Origin => _scene.Camera.Position;

        public Vector3 Angles => VectorUtils.VectorToAngles(Vector3.Transform(Camera.DefaultLookDirection, _scene.Camera.RotationMatrix));

        public Renderer(IntPtr window, IntPtr glContext, IFileSystem fileSystem, IInputSystem inputSystem, string envMapDirectory, string shadersDirectory)
        {
            _window = window;
            _glContext = glContext;

            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _envMapDirectory = envMapDirectory ?? throw new ArgumentNullException(nameof(envMapDirectory));

            _sc = new SceneContext(fileSystem, shadersDirectory);

            //Configure Veldrid graphics device
            var options = new GraphicsDeviceOptions(false, PixelFormat.R8_G8_B8_A8_UNorm, false, ResourceBindingModel.Improved, true, true);

            var platformInfo = new Veldrid.OpenGL.OpenGLPlatformInfo(
                _glContext,
                SDL.SDL_GL_GetProcAddress,
                context => SDL.SDL_GL_MakeCurrent(_window, context),
                SDL.SDL_GL_GetCurrentContext,
                () => SDL.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero),
                SDL.SDL_GL_DeleteContext,
                () => SDL.SDL_GL_SwapWindow(_window),
                sync => SDL.SDL_GL_SetSwapInterval(sync ? 1 : 0));

            SDL.SDL_GetWindowSize(_window, out var width, out var height);

            _gd = GraphicsDevice.CreateOpenGL(options, platformInfo, (uint)width, (uint)height);

            _scene = new Scene(inputSystem, _gd, width, height);

            _sc.SetCurrentScene(_scene);

            _imGuiRenderable = new ImGuiRenderable(inputSystem, width, height);
            _resizeHandled += _imGuiRenderable.WindowResized;
            _scene.AddRenderable(_imGuiRenderable);
            _scene.AddUpdateable(_imGuiRenderable);

            var coordinateAxes = new CoordinateAxes();
            _scene.AddRenderable(coordinateAxes);

            FinalPass finalPass = new FinalPass();
            _scene.AddRenderable(finalPass);

            _frameCommands = _gd.ResourceFactory.CreateCommandList();
            _frameCommands.Name = "Frame Commands List";
            CommandList initCL = _gd.ResourceFactory.CreateCommandList();
            initCL.Name = "Recreation Initialization Command List";
            initCL.Begin();
            _sc.CreateDeviceObjects(_gd, initCL, _sc);
            _scene.CreateAllDeviceObjects(_gd, initCL, _sc);
            initCL.End();
            _gd.SubmitCommands(initCL);
            initCL.Dispose();
        }

        public void WindowResized()
        {
            _windowResized = true;
        }

        public void Update(float deltaSeconds)
        {
            _scene.Update(deltaSeconds);
        }

        public void Draw()
        {
            SDL.SDL_GetWindowSize(_window, out var width, out var height);

            if (_windowResized)
            {
                _windowResized = false;

                _gd.ResizeMainWindow((uint)width, (uint)height);
                _scene.Camera.WindowResized(width, height);
                _resizeHandled?.Invoke(width, height);
                CommandList cl = _gd.ResourceFactory.CreateCommandList();
                cl.Begin();
                _sc.RecreateWindowSizedResources(_gd, cl);
                cl.End();
                _gd.SubmitCommands(cl);
                cl.Dispose();
            }

            _frameCommands.Begin();

            _scene.RenderAllStages(_gd, _frameCommands, _sc);
            _gd.SwapBuffers();
        }

        public void LoadBSP(BSPModel bspModel)
        {
            if (bspModel == null)
            {
                throw new ArgumentNullException(nameof(bspModel));
            }

            ClearBSP();

            _bspWorldRenderable = new BSPWorldRenderable(_fileSystem, bspModel, Framework.Extension.WAD);

            _scene.AddRenderable(_bspWorldRenderable);

            //TODO: define default in config
            _skyboxRenderable = Skybox2D.LoadDefaultSkybox(_fileSystem, _envMapDirectory, "2desert");
            _scene.AddRenderable(_skyboxRenderable);

            //Set up graphics data
            CommandList initCL = _gd.ResourceFactory.CreateCommandList();
            initCL.Name = "BSP Initialization Command List";
            initCL.Begin();
            _bspWorldRenderable.CreateDeviceObjects(_gd, initCL, _sc);
            _skyboxRenderable.CreateDeviceObjects(_gd, initCL, _sc);
            initCL.End();
            _gd.SubmitCommands(initCL);
            initCL.Dispose();
        }

        public void ClearBSP()
        {
            if (_bspWorldRenderable != null)
            {
                _scene.RemoveRenderable(_bspWorldRenderable);
                _bspWorldRenderable = null;
            }

            if (_skyboxRenderable != null)
            {
                _scene.RemoveRenderable(_skyboxRenderable);
                _skyboxRenderable = null;
            }

            //Clear all graphics data
            //TODO: need to create separate caches for per-map and persistent data
            _sc.ResourceCache.DestroyAllDeviceObjects();
        }
    }
}
