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

using SharpLife.FileFormats.WAD;
using SharpLife.Utility.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Models.BSP.FileFormat
{
    public static class BSPUtilities
    {
        /// <summary>
        /// Creates a WAD file from a BSP file's embedded textures
        /// The WAD file will be backed by the BSP file's textures, and changes will be reflected
        /// </summary>
        /// <param name="bspFile"></param>
        /// <returns></returns>
        public static WADFile CreateWADFromBSP(BSPFile bspFile)
        {
            if (bspFile == null)
            {
                throw new ArgumentNullException(nameof(bspFile));
            }

            return new WADFile
            {
                Version = WADVersion.WAD3,
                MipTextures = bspFile.MipTextures.Where(t => t.Data[0] != null).ToList()
            };
        }

        /// <summary>
        /// Given a BSP file and a WAD list, returns a list of textures that are referenced by the map
        /// The returned textures contain texture data
        /// </summary>
        /// <param name="bspFile"></param>
        /// <param name="wadList"></param>
        /// <returns></returns>
        public static List<MipTexture> GetUsedTextures(BSPFile bspFile, WADList wadList)
        {
            if (bspFile == null)
            {
                throw new ArgumentNullException(nameof(bspFile));
            }

            if (wadList == null)
            {
                throw new ArgumentNullException(nameof(wadList));
            }

            //BSP files contain a list of used textures, so we can just use that to filter out the ones we need
            //In the event that BSP files contain unused textures, we'd need to make a list of all used textures from the faces and then filter with that

            var names = bspFile.MipTextures;

            var usedTextures = new List<MipTexture>();

            foreach (var name in names)
            {
                var used = wadList.FindTexture(name.Name);

                //Non-embedded textures can be missing if the WAD file was not provided
                if (used != null)
                {
                    usedTextures.Add(used);
                }
            }

            return usedTextures;
        }

        /// <summary>
        /// Extracts the WAD path keyvalue from the entities string
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static string ExtractWADPathKeyValue(string entities)
        {
            if (string.IsNullOrWhiteSpace(entities))
            {
                throw new ArgumentException(nameof(entities));
            }

            //Scan the worldspawn entity for the wad keyvalue
            var worldspawnData = KeyValuesParser.Parse(new Tokenizer(entities));

            var wadPathIndex = worldspawnData.FindIndex(kp => kp.Key == "wad");

            if (wadPathIndex != -1)
            {
                return worldspawnData[wadPathIndex].Value;
            }

            throw new InvalidOperationException("No wadpath keyvalue found");
        }
    }
}
