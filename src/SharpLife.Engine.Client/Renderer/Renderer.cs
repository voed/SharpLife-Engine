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
using SharpLife.FileFormats.BSP;
using SharpLife.FileFormats.WAD;
using SharpLife.FileSystem;
using SharpLife.Input;
using SharpLife.Models;
using SharpLife.Models.BSP;
using SharpLife.Renderer;
using SharpLife.Renderer.BSP;
using SharpLife.Renderer.Models;
using SharpLife.Renderer.Objects;
using SharpLife.Renderer.StudioModel;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using Veldrid;

namespace SharpLife.Engine.Client.Renderer
{
    /// <summary>
    /// The main renderer
    /// Manages current graphics state, devices, etc
    /// </summary>
    public class Renderer
    {
        private readonly IntPtr _window;
        private readonly IntPtr _glContext;
        private readonly GraphicsDevice _gd;
        private readonly IFileSystem _fileSystem;

        private readonly IRendererListener _rendererListener;

        private readonly string _envMapDirectory;

        private readonly SceneContext _sc;

        private readonly ImGuiRenderable _imGuiRenderable;

        private CoordinateAxes _coordinateAxes;

        private readonly FinalPass _finalPass;

        private Skybox2D _skyboxRenderable;

        private readonly CommandList _frameCommands;

        private bool _windowResized = false;

        private event Action<int, int> _resizeHandled;

        public Scene Scene { get; }

        private readonly ModelResourcesManager _modelResourcesManager;
        private readonly ModelRenderer _modelRenderer;

        public Renderer(
            IntPtr window, IntPtr glContext,
            IFileSystem fileSystem, IInputSystem inputSystem, IRendererListener rendererListener,
            string envMapDirectory, string shadersDirectory)
        {
            _window = window;
            _glContext = glContext;

            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _rendererListener = rendererListener ?? throw new ArgumentNullException(nameof(rendererListener));
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

            Scene = new Scene(inputSystem, _gd, width, height);

            _sc.SetCurrentScene(Scene);

            _imGuiRenderable = new ImGuiRenderable(inputSystem, width, height);
            _resizeHandled += _imGuiRenderable.WindowResized;
            Scene.AddContainer(_imGuiRenderable);
            Scene.AddRenderable(_imGuiRenderable);
            Scene.AddUpdateable(_imGuiRenderable);

            _finalPass = new FinalPass();
            Scene.AddContainer(_finalPass);
            Scene.AddRenderable(_finalPass);

            _modelResourcesManager = new ModelResourcesManager(
                new Dictionary<Type, IModelResourceFactory>
                {
                    { typeof(BSPModel), new BSPModelResourceFactory() },
                    { typeof(StudioModel), new StudioModelResourceFactory() }
                });
            _modelRenderer = new ModelRenderer(
                _modelResourcesManager,
                (modelRenderer, viewState) => _rendererListener.OnRenderModels(modelRenderer, viewState)
                );

            foreach (var factory in _modelResourcesManager.Factories)
            {
                Scene.AddContainer(factory);
            }

            Scene.AddRenderable(_modelRenderer);

            _frameCommands = _gd.ResourceFactory.CreateCommandList();
            _frameCommands.Name = "Frame Commands List";
            CommandList initCL = _gd.ResourceFactory.CreateCommandList();
            initCL.Name = "Recreation Initialization Command List";
            initCL.Begin();
            _sc.CreateDeviceObjects(_gd, initCL, _sc);
            Scene.CreateAllDeviceObjects(_gd, initCL, _sc, ResourceScope.Global);
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
            Scene.Update(deltaSeconds);
        }

        public void Draw()
        {
            SDL.SDL_GetWindowSize(_window, out var width, out var height);

            if (_windowResized)
            {
                _windowResized = false;

                _gd.ResizeMainWindow((uint)width, (uint)height);
                Scene.Camera.WindowResized(width, height);
                _resizeHandled?.Invoke(width, height);
                CommandList cl = _gd.ResourceFactory.CreateCommandList();
                cl.Begin();
                _sc.RecreateWindowSizedResources(_gd, cl);
                cl.End();
                _gd.SubmitCommands(cl);
                cl.Dispose();
            }

            _frameCommands.Begin();

            Scene.RenderAllStages(_gd, _frameCommands, _sc);
            _gd.SwapBuffers();
        }

        private void UploadWADTextures(BSPModel worldModel)
        {
            //Load all WADs
            var wadList = new WADList(_fileSystem, Framework.Extension.WAD);

            var embeddedTexturesWAD = BSPUtilities.CreateWADFromBSP(worldModel.BSPFile);

            wadList.Add(worldModel.Name + FileExtensionUtils.AsExtension(Framework.Extension.WAD), embeddedTexturesWAD);

            var wadPath = BSPUtilities.ExtractWADPathKeyValue(worldModel.BSPFile.Entities);

            foreach (var wadName in wadPath.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var baseName = Path.GetFileNameWithoutExtension(wadName);

                //Never allow these to be loaded, they contain spray decals
                //TODO: refactor into blacklist
                if (baseName != "pldecal" && baseName != "tempdecal")
                {
                    //WAD loading only needs to consider the filename; the directory part is mapper specific
                    var fileName = Path.GetFileName(wadName);
                    wadList.Load(fileName);
                }
            }

            //Upload all used textures
            var usedTextures = BSPUtilities.GetUsedTextures(worldModel.BSPFile, wadList);

            WADUtilities.UploadTextures(_gd, _gd.ResourceFactory, _sc.MapResourceCache, usedTextures);
        }

        /// <summary>
        /// Loads all models in the model manager
        /// This is when BSP models are loaded
        /// </summary>
        /// <param name="worldModel"></param>
        /// <param name="modelManager"></param>
        public void LoadModels(BSPModel worldModel, IModelManager modelManager)
        {
            if (worldModel == null)
            {
                throw new ArgumentNullException(nameof(worldModel));
            }

            if (modelManager == null)
            {
                throw new ArgumentNullException(nameof(modelManager));
            }

            ClearBSP();

            UploadWADTextures(worldModel);

            foreach (var model in modelManager)
            {
                var modelRenderable = _modelResourcesManager.CreateResources(model);

                Scene.AddContainer(modelRenderable);
            }

            _coordinateAxes = new CoordinateAxes();
            Scene.AddContainer(_coordinateAxes);
            Scene.AddRenderable(_coordinateAxes);

            //TODO: define default in config
            _skyboxRenderable = Skybox2D.LoadDefaultSkybox(_fileSystem, _envMapDirectory, "2desert");
            Scene.AddContainer(_skyboxRenderable);
            Scene.AddRenderable(_skyboxRenderable);

            //Set up graphics data
            CommandList initCL = _gd.ResourceFactory.CreateCommandList();
            initCL.Name = "Model Initialization Command List";
            initCL.Begin();

            Scene.CreateAllDeviceObjects(_gd, initCL, _sc, ResourceScope.Map);

            initCL.End();
            _gd.SubmitCommands(initCL);
            initCL.Dispose();
        }

        public void ClearBSP()
        {
            Scene.DestroyAllDeviceObjects(ResourceScope.Map);

            foreach (var resource in _modelResourcesManager)
            {
                Scene.RemoveContainer(resource);
            }

            _modelResourcesManager.FreeAllResources();

            if (_skyboxRenderable != null)
            {
                Scene.RemoveContainer(_skyboxRenderable);
                Scene.RemoveRenderable(_skyboxRenderable);
                _skyboxRenderable.Dispose();
                _skyboxRenderable = null;
            }

            if (_coordinateAxes != null)
            {
                Scene.RemoveContainer(_coordinateAxes);
                Scene.RemoveRenderable(_coordinateAxes);
                _coordinateAxes.Dispose();
                _coordinateAxes = null;
            }

            //Clear all graphics data
            _sc.MapResourceCache.DestroyAllDeviceObjects();
        }
    }
}
