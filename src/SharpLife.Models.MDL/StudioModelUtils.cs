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

using SharpLife.Models.MDL.FileFormat;
using System;

namespace SharpLife.Models.MDL
{
    public static class StudioModelUtils
    {
        /// <summary>
        /// Gets the currently selected submodel for the given body group
        /// </summary>
        /// <param name="studioFile"></param>
        /// <param name="currentPackedValue"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public static uint GetBodyGroupValue(StudioFile studioFile, uint currentPackedValue, uint group)
        {
            if (studioFile == null)
            {
                throw new ArgumentNullException(nameof(studioFile));
            }

            if (group >= studioFile.BodyParts.Count)
            {
                return 0;
            }

            var bodyPart = studioFile.BodyParts[(int)group];

            if (bodyPart.Models.Count <= 1)
            {
                return 0;
            }

            return (uint)((currentPackedValue / bodyPart.Base) % bodyPart.Models.Count);
        }

        /// <summary>
        /// Given the current packed value, calculates the new packed value for the given body group and submodel
        /// </summary>
        /// <param name="studioFile"></param>
        /// <param name="currentPackedValue"></param>
        /// <param name="group"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static uint CalculateBodyGroupValue(StudioFile studioFile, uint currentPackedValue, uint group, uint newValue)
        {
            if (studioFile == null)
            {
                throw new ArgumentNullException(nameof(studioFile));
            }

            if (group >= studioFile.BodyParts.Count)
            {
                return currentPackedValue;
            }

            var bodyPart = studioFile.BodyParts[(int)group];

            if (newValue >= bodyPart.Models.Count)
            {
                return currentPackedValue;
            }

            var current = (currentPackedValue / bodyPart.Base) % bodyPart.Models.Count;

            return (uint)(currentPackedValue - (current * bodyPart.Base) + (newValue * bodyPart.Base));
        }
    }
}
