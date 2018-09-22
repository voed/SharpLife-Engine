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

namespace SharpLife.Game.Shared.Models
{
    public enum RenderFX
    {
        None = 0,
        PulseSlow,
        PulseFast,
        PulseSlowWide,
        PulseFastWide,
        FadeSlow,
        FadeFast,
        SolidSlow,
        SolidFast,
        StrobeSlow,
        StrobeFast,
        StrobeFaster,
        FlickerSlow,
        FlickerFast,
        NoDissipation,

        /// <summary>
        /// Distort/scale/translate flicker
        /// </summary>
        Distort,

        /// <summary>
        /// kRenderFxDistort + distance fade
        /// </summary>
        Hologram,

        /// <summary>
        /// kRenderAmt is the player index
        /// TODO don't use render amount for this
        /// </summary>
        DeadPlayer,

        /// <summary>
        /// Scale up really big!
        /// </summary>
        Explode,

        /// <summary>
        /// Glowing Shell
        /// </summary>
        GlowShell,

        /// <summary>
        /// Keep this sprite from getting very small (SPRITES only!)
        /// </summary>
        ClampMinScale,

        /// <summary>
        /// CTM !!!CZERO added to tell the studiorender that the value in iuser2 is a lightmultiplier
        /// TODO need to use something other than iuser2
        /// </summary>
        LightMultiplier,
    }
}
