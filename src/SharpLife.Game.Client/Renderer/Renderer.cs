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
using Serilog;
using SharpLife.CommandSystem;
using SharpLife.Engine.Shared;
using SharpLife.Engine.Shared.UI;
using SharpLife.FileFormats.WAD;
using SharpLife.FileSystem;
using SharpLife.Game.Client.Renderer.Shared;
using SharpLife.Game.Client.Renderer.Shared.Models;
using SharpLife.Game.Client.Renderer.Shared.Models.BSP;
using SharpLife.Game.Client.Renderer.Shared.Models.MDL;
using SharpLife.Game.Client.Renderer.Shared.Models.SPR;
using SharpLife.Game.Client.Renderer.Shared.Objects;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Game.Shared.Models.SPR;
using SharpLife.Input;
using SharpLife.Models;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.BSP.Rendering;
using SharpLife.Utility;
using SharpLife.Utility.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.OpenGL;

namespace SharpLife.Game.Client.Renderer
{
    /// <summary>
    /// The main renderer
    /// Manages current graphics state, devices, etc
    /// </summary>
    public class Renderer : IRenderer
    {
        private readonly IWindow _window;

        private readonly ILogger _logger;

        private readonly GraphicsDevice _gd;
        private readonly IFileSystem _fileSystem;

        private readonly ITime _engineTime;

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

        public Vector3 SkyColor
        {
            get => Scene.SkyColor;
            set => Scene.SkyColor = value;
        }

        public Vector3 SkyNormal
        {
            get => Scene.SkyNormal;
            set => Scene.SkyNormal = value;
        }

        public Scene Scene { get; }

        private readonly ModelResourcesManager _modelResourcesManager;
        private readonly ModelRenderer _modelRenderer;

        public Renderer(
            IWindow window,
            ILogger logger,
            IFileSystem fileSystem, ICommandContext commandContext, IInputSystem inputSystem,
            ITime engineTime,
            IRendererListener rendererListener,
            string envMapDirectory, string shadersDirectory)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));
            _rendererListener = rendererListener ?? throw new ArgumentNullException(nameof(rendererListener));
            _envMapDirectory = envMapDirectory ?? throw new ArgumentNullException(nameof(envMapDirectory));

            //Configure Veldrid graphics device
            //Don't use a swap chain depth format, it won't render anything on Vulkan
            //It isn't needed right now so it should be disabled for the time being
            var options = new GraphicsDeviceOptions(false, null/*PixelFormat.R8_G8_B8_A8_UNorm*/, false, ResourceBindingModel.Improved, true, true);

            _gd = CreateGraphicsDevice(options, GraphicsBackend.OpenGL);

            _gd.SyncToVerticalBlank = false;

            _window.GetSize(out var width, out var height);

            Scene = new Scene(inputSystem, commandContext, _gd, width, height);

            _imGuiRenderable = new ImGuiRenderable(inputSystem, width, height);
            _resizeHandled += _imGuiRenderable.WindowResized;
            Scene.AddContainer(_imGuiRenderable);
            Scene.AddRenderable(_imGuiRenderable);
            Scene.AddUpdateable(_imGuiRenderable);

            _finalPass = new FinalPass();
            Scene.AddContainer(_finalPass);
            Scene.AddRenderable(_finalPass);

            var spriteRenderer = new SpriteModelRenderer(_logger);
            var studioRenderer = new StudioModelRenderer(commandContext);
            var brushRenderer = new BrushModelRenderer();

            _modelResourcesManager = new ModelResourcesManager(new Dictionary<Type, ModelResourcesManager.ResourceFactory>
            {
                {typeof(SpriteModel), model => new SpriteModelResourceContainer((SpriteModel)model) },
                { typeof(StudioModel), model => new StudioModelResourceContainer((StudioModel)model)  },
                { typeof(BSPModel), model => new BSPModelResourceContainer((BSPModel)model)  }
            });

            _modelRenderer = new ModelRenderer(
                _modelResourcesManager,
                (modelRenderer, viewState) => _rendererListener.OnRenderModels(modelRenderer, viewState),
                spriteRenderer,
                studioRenderer,
                brushRenderer
                );

            Scene.AddRenderable(_modelRenderer);

            Scene.AddContainer(spriteRenderer);
            Scene.AddContainer(studioRenderer);
            Scene.AddContainer(brushRenderer);

            _sc = new SceneContext(fileSystem, commandContext, _modelRenderer, shadersDirectory);

            _sc.SetCurrentScene(Scene);

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

        private static SwapchainSource GetSwapchainSource(IntPtr window)
        {
            SDL.SDL_SysWMinfo sysWmInfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_GetVersion(out sysWmInfo.version);
            SDL.SDL_GetWindowWMInfo(window, ref sysWmInfo);
            switch (sysWmInfo.subsystem)
            {
                case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS:
                    ref var w32Info = ref sysWmInfo.info.win;
                    return SwapchainSource.CreateWin32(w32Info.window, w32Info.hdc);
                case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11:
                    ref var x11Info = ref sysWmInfo.info.x11;
                    return SwapchainSource.CreateXlib(
                        x11Info.display,
                        x11Info.window);
                case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_COCOA:
                    ref var cocoaInfo = ref sysWmInfo.info.cocoa;
                    var nsWindow = cocoaInfo.window;
                    return SwapchainSource.CreateNSWindow(nsWindow);
                default:
                    throw new PlatformNotSupportedException("Cannot create a SwapchainSource for " + sysWmInfo.subsystem + ".");
            }
        }

        private GraphicsDevice CreateOpenGLGraphicsDevice(GraphicsDeviceOptions options)
        {
            _window.GetSize(out var width, out var height);

            var glContextHandle = SDL.SDL_GL_CreateContext(_window.WindowHandle);

            if (glContextHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create SDL Window");
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, out var r))
            {
                r = 0;
                _logger.Information("Failed to get GL RED size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, out var g))
            {
                g = 0;
                _logger.Information("Failed to get GL GREEN size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, out var b))
            {
                b = 0;
                _logger.Information("Failed to get GL BLUE size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, out var a))
            {
                a = 0;
                _logger.Information("Failed to get GL ALPHA size ({0})", SDL.SDL_GetError());
            }

            if (0 != SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, out var depth))
            {
                depth = 0;
                _logger.Information("Failed to get GL DEPTH size ({0})", SDL.SDL_GetError());
            }

            _logger.Information($"GL_SIZES:  r:{r} g:{g} b:{b} a:{a} depth:{depth}");

            if (r <= 4 || g <= 4 || b <= 4 || depth <= 15 /*|| gl_renderer && Q_strstr(gl_renderer, "GDI Generic")*/)
            {
                throw new InvalidOperationException("Failed to create SDL Window, unsupported video mode. A 16-bit color depth desktop is required and a supported GL driver");
            }

            var platformInfo = new OpenGLPlatformInfo(
                glContextHandle,
                SDL.SDL_GL_GetProcAddress,
                context => SDL.SDL_GL_MakeCurrent(_window.WindowHandle, context),
                SDL.SDL_GL_GetCurrentContext,
                () => SDL.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero),
                SDL.SDL_GL_DeleteContext,
                () => SDL.SDL_GL_SwapWindow(_window.WindowHandle),
                sync => SDL.SDL_GL_SetSwapInterval(sync ? 1 : 0));

            return GraphicsDevice.CreateOpenGL(options, platformInfo, (uint)width, (uint)height);
        }

        private GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options)
        {
            _window.GetSize(out var width, out var height);

            var swapChainDescription = new SwapchainDescription(
                GetSwapchainSource(_window.WindowHandle),
                (uint)width,
                (uint)height,
                options.SwapchainDepthFormat,
                false
                );

            return GraphicsDevice.CreateVulkan(options, swapChainDescription);
        }

        private GraphicsDevice CreateGraphicsDevice(GraphicsDeviceOptions options, GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.OpenGL:
                    return CreateOpenGLGraphicsDevice(options);

                case GraphicsBackend.Vulkan:
                    return CreateVulkanGraphicsDevice(options);

                default: throw new NotSupportedException($"Graphics backend {backend} not supported");
            }
        }

        public void WindowResized()
        {
            _windowResized = true;
        }

        public void Update(float deltaSeconds)
        {
            Scene.Update(_engineTime, deltaSeconds);
        }

        public void Draw()
        {
            _window.GetSize(out var width, out var height);

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

            WADUtilities.UploadTextures(usedTextures, _sc.TextureLoader, _gd, _sc.MapResourceCache);
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

            Scene.WorldModel = worldModel;

            //Reset light styles
            Scene.InitializeLightStyles();

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

            Scene.WorldModel = null;
        }
    }
}
