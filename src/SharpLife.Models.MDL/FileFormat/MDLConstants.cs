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

namespace SharpLife.Models.MDL.FileFormat
{
    public static class MDLConstants
    {
        /// <summary>
        /// Identifier for the main and texture studio model headers
        /// </summary>
        public const int MainHeaderIdentifier = ((byte)'T' << 24) + ((byte)'S' << 16) + ((byte)'D' << 8) + (byte)'I';

        public const int SequenceHeaderIdentifier = ((byte)'Q' << 24) + ((byte)'S' << 16) + ((byte)'D' << 8) + (byte)'I';

        //XYZ translation, XYZ rotation
        public const int NumAxes = 6;

        public const int NumBlendTypes = 2;

        public const int MaxBones = 128;

        public const int MaxControllers = 8;

        public const int MouthControllerIndex = 4;

        public const int MaxBlenders = 2;

        public const int NoBoneController = -1;

        public const int NoBoneParent = -1;
    }
}
