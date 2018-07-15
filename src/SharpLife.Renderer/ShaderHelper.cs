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
using System.IO;
using Veldrid;
using Veldrid.SPIRV;

namespace SharpLife.Renderer
{
    public class ShaderHelper
    {
        private readonly IFileSystem _fileSystem;

        private readonly string _shadersDirectory;

        public ShaderHelper(IFileSystem fileSystem, string shadersDirectory)
        {
            _fileSystem = fileSystem;
            _shadersDirectory = shadersDirectory ?? throw new ArgumentNullException(nameof(shadersDirectory));
        }

        public (Shader vs, Shader fs) LoadSPIRV(
            GraphicsDevice gd,
            ResourceFactory factory,
            string setName)
        {
            byte[] vsBytes = LoadBytecode(GraphicsBackend.Vulkan, setName, ShaderStages.Vertex);
            byte[] fsBytes = LoadBytecode(GraphicsBackend.Vulkan, setName, ShaderStages.Fragment);
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            Shader[] shaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, vsBytes, "main", debug),
                new ShaderDescription(ShaderStages.Fragment, fsBytes, "main", debug),
                GetOptions(gd));

            Shader vs = shaders[0];
            Shader fs = shaders[1];

            vs.Name = setName + "-Vertex";
            fs.Name = setName + "-Fragment";

            return (vs, fs);
        }

        private CrossCompileOptions GetOptions(
            GraphicsDevice gd)
        {
            bool fixClipZ = false;
            const bool invertY = false;
            List<SpecializationConstant> specializations = new List<SpecializationConstant>
            {
                new SpecializationConstant(102, gd.IsDepthRangeZeroToOne)
            };
            switch (gd.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                case GraphicsBackend.Metal:
                    specializations.Add(new SpecializationConstant(100, false));
                    break;
                case GraphicsBackend.Vulkan:
                    specializations.Add(new SpecializationConstant(100, true));
                    break;
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    specializations.Add(new SpecializationConstant(100, false));
                    specializations.Add(new SpecializationConstant(101, true));
                    fixClipZ = !gd.IsDepthRangeZeroToOne;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return new CrossCompileOptions(fixClipZ, invertY, specializations.ToArray());
        }

        public byte[] LoadBytecode(GraphicsBackend backend, string setName, ShaderStages stage)
        {
            string stageExt = stage == ShaderStages.Vertex ? "vert" : "frag";
            string name = setName + "." + stageExt;

            if (backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.Direct3D11)
            {
                string bytecodeExtension = GetBytecodeExtension(backend);
                string bytecodePath = Path.Combine(_shadersDirectory, name + bytecodeExtension);
                if (_fileSystem.Exists(bytecodePath))
                {
                    return _fileSystem.ReadAllBytes(bytecodePath);
                }
            }

            string extension = GetSourceExtension(backend);
            string path = Path.Combine("Shaders.Generated", name + extension);
            return _fileSystem.ReadAllBytes(path);
        }

        private static string GetBytecodeExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return ".hlsl.bytes";
                case GraphicsBackend.Vulkan: return ".spv";
                case GraphicsBackend.OpenGL:
                    throw new InvalidOperationException("OpenGL and OpenGLES do not support shader bytecode.");
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }

        private static string GetSourceExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return ".hlsl";
                case GraphicsBackend.Vulkan: return ".450.glsl";
                case GraphicsBackend.OpenGL:
                    return ".330.glsl";
                case GraphicsBackend.OpenGLES:
                    return ".300.glsles";
                case GraphicsBackend.Metal:
                    return ".metallib";
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }
    }
}
