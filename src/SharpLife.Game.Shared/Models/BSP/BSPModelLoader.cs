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
using SharpLife.Models;
using SharpLife.Models.BSP.FileFormat;
using System;
using System.IO;
using System.Numerics;

namespace SharpLife.Game.Shared.Models.BSP
{
    public sealed class BSPModelLoader : IModelLoader
    {
        private readonly string _bspModelNamePrefix;

        public BSPModelLoader(string bspModelNamePrefix)
        {
            _bspModelNamePrefix = bspModelNamePrefix ?? throw new ArgumentNullException(nameof(bspModelNamePrefix));
        }

        public IModel Load(string name, IFileSystem fileSystem, BinaryReader reader, Delegates.AddModel addModelCallback, bool computeCRC)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            //Check if we can actually load this
            //TODO: because BSP files don't have a separate identifier, this will fail on invalid BSP versions
            //Should remove this once the other formats can be loaded
            if (!BSPLoader.IsBSPFile(reader))
            {
                return null;
            }

            var loader = new BSPLoader(reader);

            var bspFile = loader.ReadBSPFile();

            uint crc = 0;

            if (computeCRC)
            {
                crc = loader.ComputeCRC();
            }

            var hull0 = MakeHull0(bspFile);

            //add all of its submodels
            //First submodel (0) is the world
            for (var i = 1; i < bspFile.Models.Count; ++i)
            {
                var subModelName = $"{_bspModelNamePrefix}{i}";
                addModelCallback(subModelName, new BSPModel(subModelName, crc, bspFile, bspFile.Models[i], hull0));
            }

            return new BSPModel(name, crc, bspFile, bspFile.Models[0], hull0);
        }

        /// <summary>
        /// Create a clipping hull out of the visible hull
        /// </summary>
        /// <param name="bspFile"></param>
        /// <returns></returns>
        private Hull MakeHull0(BSPFile bspFile)
        {
            var clipNodes = new ClipNode[bspFile.Nodes.Count];

            for (var i = 0; i < bspFile.Nodes.Count; ++i)
            {
                var node = bspFile.Nodes[i];

                var clipNode = new ClipNode
                {
                    PlaneIndex = Array.FindIndex(bspFile.Planes, plane => ReferenceEquals(plane, node.Plane))
                };

                for (var j = 0; j < 2; ++j)
                {
                    var child = node.Children[j];

                    if (child.Contents >= Contents.Node)
                    {
                        clipNode.Children[j] = bspFile.Nodes.FindIndex(test => ReferenceEquals(test, child));
                    }
                    else
                    {
                        clipNode.Children[j] = (int)child.Contents;
                    }
                }

                clipNodes[i] = clipNode;
            }

            return new Hull(0, bspFile.Nodes.Count - 1, Vector3.Zero, Vector3.Zero, clipNodes, bspFile.Planes);
        }
    }
}
