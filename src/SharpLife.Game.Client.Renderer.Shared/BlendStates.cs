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

using Veldrid;

namespace SharpLife.Game.Client.Renderer.Shared
{
    public static class BlendStates
    {
        /// <summary>
        /// Describes a blend attachment state in which the source is added to the destination.
        /// Settings:
        ///     BlendEnabled = true
        ///     SourceColorFactor = BlendFactor.SourceAlpha
        ///     DestinationColorFactor = BlendFactor.One
        ///     ColorFunction = BlendFunction.Add
        ///     SourceAlphaFactor = BlendFactor.SourceAlpha
        ///     DestinationAlphaFactor = BlendFactor.One
        ///     AlphaFunction = BlendFunction.Add
        /// </summary>
        public static readonly BlendAttachmentDescription AdditiveOneOneBlend = new BlendAttachmentDescription
        {
            BlendEnabled = true,
            SourceColorFactor = BlendFactor.One,
            DestinationColorFactor = BlendFactor.One,
            ColorFunction = BlendFunction.Add,
            SourceAlphaFactor = BlendFactor.One,
            DestinationAlphaFactor = BlendFactor.One,
            AlphaFunction = BlendFunction.Add,
        };

        /// <summary>
        /// Describes a blend state in which a single color target is blended with <see cref="AdditiveOneOneBlend"/>.
        /// </summary>
        public static readonly BlendStateDescription SingleAdditiveOneOneBlend = new BlendStateDescription
        {
            AttachmentStates = new[] { AdditiveOneOneBlend }
        };
    }
}
