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
using SharpLife.Models.SPR;

namespace SharpLife.Game.Server.Entities.Effects
{
    [LinkEntityToClass("env_sprite")]
    [Networkable(UseBaseType = true)]
    public class EnvSprite : NetworkedEntity
    {
        private float _lastTime;

        //TODO: implement

        public override void Think()
        {
            if (Model is SpriteModel spriteModel)
            {
                Frame += (float)(FrameRate * (Context.Time.ElapsedTime - _lastTime));

                if (Frame >= spriteModel.SpriteFile.Frames.Count)
                {
                    Frame %= spriteModel.SpriteFile.Frames.Count;
                }

                _lastTime = (float)Context.Time.ElapsedTime;
            }
        }
    }
}
