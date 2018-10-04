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

using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using System.Numerics;

namespace SharpLife.Game.Client.Entities.Lighting
{
    [LinkEntityToClass("light_environment")]
    [Networkable]
    public class LightEnvironment : Light
    {
        [Networked]
        public Vector3 SkyColor { get; set; }

        [Networked]
        public Vector3 SkyNormal { get; set; }

        public override void OnEndUpdate()
        {
            base.OnEndUpdate();

            //Update sky values if they have changed
            if (SkyColor != Context.Renderer.SkyColor)
            {
                Context.Renderer.SkyColor = SkyColor;
            }

            if (SkyNormal != Context.Renderer.SkyNormal)
            {
                Context.Renderer.SkyNormal = SkyNormal;
            }
        }
    }
}
