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

namespace SharpLife.Game.Server.Entities
{
    [LinkEntityToClass("worldspawn")]
    [Networkable]
    public sealed class World : NetworkedEntity
    {
        public override void OnConstruct()
        {
            base.OnConstruct();

            Context.Entities.World = this;
        }

        public override void OnDestruct()
        {
            Context.Entities.World = null;

            base.OnDestruct();
        }

        public override void Precache()
        {
            base.Precache();

            Model = Context.MapInfo.Model;
        }

        protected override void Spawn()
        {
            Precache();
        }
    }
}
