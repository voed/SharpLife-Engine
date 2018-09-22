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
    public enum RenderMode
    {
        Normal,             // src
        TransColor,         // c*a+dest*(1-a)
        TransTexture,       // src*a+dest*(1-a)
        Glow,               // src*a+dest -- No Z buffer checks
        TransAlpha,         // src*srca+dest*(1-srca)
        TransAdd,		    // src*a+dest

        First = Normal,
        Last = TransAdd,
    }
}
