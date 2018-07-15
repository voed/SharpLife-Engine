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

using SharpLife.FileSystem;
using System;
using System.Collections.Generic;
using Veldrid;
using Veldrid.ImageSharp;

namespace SharpLife.Renderer
{
    // Non-thread-safe cache for resources.
    public class ResourceCache
    {
        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> s_pipelines
            = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        private readonly Dictionary<ResourceLayoutDescription, ResourceLayout> s_layouts
            = new Dictionary<ResourceLayoutDescription, ResourceLayout>();

        private readonly Dictionary<(string, ShaderStages), Shader> s_shaders
            = new Dictionary<(string, ShaderStages), Shader>();

        private readonly Dictionary<string, (Shader, Shader)> s_shaderSets
            = new Dictionary<string, (Shader, Shader)>();

        private readonly Dictionary<ImageSharpTexture, Texture> s_textures
            = new Dictionary<ImageSharpTexture, Texture>();

        private readonly Dictionary<Texture, TextureView> s_textureViews = new Dictionary<Texture, TextureView>();

        private readonly Dictionary<ResourceSetDescription, ResourceSet> s_resourceSets
            = new Dictionary<ResourceSetDescription, ResourceSet>();

        private Texture _pinkTex;

        public readonly ResourceLayoutDescription ProjViewLayoutDescription = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex));

        private readonly ShaderHelper _shaderHelper;

        public ResourceCache(IFileSystem fileSystem, string shadersDirectory)
        {
            _shaderHelper = new ShaderHelper(fileSystem, shadersDirectory);
        }

        public Pipeline GetPipeline(ResourceFactory factory, ref GraphicsPipelineDescription desc)
        {
            if (!s_pipelines.TryGetValue(desc, out Pipeline p))
            {
                p = factory.CreateGraphicsPipeline(ref desc);
                s_pipelines.Add(desc, p);
            }

            return p;
        }

        public ResourceLayout GetResourceLayout(ResourceFactory factory, ResourceLayoutDescription desc)
        {
            if (!s_layouts.TryGetValue(desc, out ResourceLayout p))
            {
                p = factory.CreateResourceLayout(ref desc);
                s_layouts.Add(desc, p);
            }

            return p;
        }

        public (Shader vs, Shader fs) GetShaders(
            GraphicsDevice gd,
            ResourceFactory factory,
            string name)
        {
            if (!s_shaderSets.TryGetValue(name, out (Shader vs, Shader fs) set))
            {
                set = _shaderHelper.LoadSPIRV(gd, factory, name);
                s_shaderSets.Add(name, set);
            }

            return set;
        }

        public void DestroyAllDeviceObjects()
        {
            foreach (KeyValuePair<GraphicsPipelineDescription, Pipeline> kvp in s_pipelines)
            {
                kvp.Value.Dispose();
            }
            s_pipelines.Clear();

            foreach (KeyValuePair<ResourceLayoutDescription, ResourceLayout> kvp in s_layouts)
            {
                kvp.Value.Dispose();
            }
            s_layouts.Clear();

            foreach (KeyValuePair<(string, ShaderStages), Shader> kvp in s_shaders)
            {
                kvp.Value.Dispose();
            }
            s_shaders.Clear();

            foreach (KeyValuePair<string, (Shader, Shader)> kvp in s_shaderSets)
            {
                kvp.Value.Item1.Dispose();
                kvp.Value.Item2.Dispose();
            }
            s_shaderSets.Clear();

            foreach (KeyValuePair<ImageSharpTexture, Texture> kvp in s_textures)
            {
                kvp.Value.Dispose();
            }
            s_textures.Clear();

            foreach (KeyValuePair<Texture, TextureView> kvp in s_textureViews)
            {
                kvp.Value.Dispose();
            }
            s_textureViews.Clear();

            _pinkTex?.Dispose();
            _pinkTex = null;

            foreach (KeyValuePair<ResourceSetDescription, ResourceSet> kvp in s_resourceSets)
            {
                kvp.Value.Dispose();
            }
            s_resourceSets.Clear();
        }

        internal Texture GetTexture2D(GraphicsDevice gd, ResourceFactory factory, ImageSharpTexture textureData)
        {
            if (!s_textures.TryGetValue(textureData, out Texture tex))
            {
                tex = textureData.CreateDeviceTexture(gd, factory);
                s_textures.Add(textureData, tex);
            }

            return tex;
        }

        internal TextureView GetTextureView(ResourceFactory factory, Texture texture)
        {
            if (!s_textureViews.TryGetValue(texture, out TextureView view))
            {
                view = factory.CreateTextureView(texture);
                s_textureViews.Add(texture, view);
            }

            return view;
        }

        internal unsafe Texture GetPinkTexture(GraphicsDevice gd, ResourceFactory factory)
        {
            if (_pinkTex == null)
            {
                RgbaByte pink = RgbaByte.Pink;
                _pinkTex = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                gd.UpdateTexture(_pinkTex, (IntPtr)(&pink), 4, 0, 0, 0, 1, 1, 1, 0, 0);
            }

            return _pinkTex;
        }

        internal ResourceSet GetResourceSet(ResourceFactory factory, ResourceSetDescription description)
        {
            if (!s_resourceSets.TryGetValue(description, out ResourceSet ret))
            {
                ret = factory.CreateResourceSet(ref description);
                s_resourceSets.Add(description, ret);
            }

            return ret;
        }
    }
}
