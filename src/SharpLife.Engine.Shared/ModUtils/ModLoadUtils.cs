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
using System.IO;
using System.Reflection;

namespace SharpLife.Engine.Shared.ModUtils
{
    public static class ModLoadUtils
    {
        /// <summary>
        /// Loads a mod assembly and creates an instance of its entry point
        /// </summary>
        /// <typeparam name="TEntryPoint"></typeparam>
        /// <param name="gameDirectory"></param>
        /// <param name="assemblyName"></param>
        /// <param name="entrypointClass"></param>
        /// <returns></returns>
        public static ModData<TEntryPoint> LoadMod<TEntryPoint>(string gameDirectory, string assemblyName, string entrypointClass)
        {
            if (gameDirectory == null)
            {
                throw new ArgumentNullException(nameof(gameDirectory));
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (entrypointClass == null)
            {
                throw new ArgumentNullException(nameof(entrypointClass));
            }

            var path = Path.Combine(gameDirectory, assemblyName);

            var assembly = Assembly.LoadFrom(path);

            var entrypointType = assembly.GetType(entrypointClass);

            if (entrypointType == null)
            {
                throw new InvalidOperationException($"Couldn't find entry point {entrypointClass} in mod assembly {assemblyName}");
            }

            var entryPointBaseType = typeof(TEntryPoint);

            var isEntryPointImplementation = entryPointBaseType.IsInterface ?
                entrypointType.GetInterface(entryPointBaseType.FullName) != null :
                entrypointType.IsSubclassOf(entrypointType);

            if (!isEntryPointImplementation)
            {
                throw new InvalidOperationException(
                    $"Entry point \"{entrypointClass}\" in mod assembly \"{assemblyName}\" does not implement entry point type \"{entryPointBaseType.FullName}\"");
            }

            var entrypoint = (TEntryPoint)Activator.CreateInstance(entrypointType);

            return new ModData<TEntryPoint>
            {
                assembly = assembly,
                Entrypoint = entrypoint
            };
        }
    }
}
