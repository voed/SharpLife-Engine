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

using SharpLife.Game.Client.Renderer.Shared;

namespace SharpLife.Game.Client.UI
{
    /// <summary>
    /// The client UI, used to allow drawing
    /// </summary>
    public interface IClientUI
    {
        void Update(float deltaSeconds, IViewState viewState);

        void Draw(IViewState viewState);
    }
}
