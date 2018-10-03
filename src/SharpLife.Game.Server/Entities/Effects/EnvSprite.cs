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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Models.SPR;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;

namespace SharpLife.Game.Server.Entities.Effects
{
    [LinkEntityToClass("env_sprite")]
    [Networkable]
    public class EnvSprite : NetworkedEntity
    {
        [Networked(TypeConverterType = typeof(FloatToIntConverter))]
        [BitConverterOptions(10, 4)]
        public float Frame { get; set; }

        [Networked]
        public float FrameRate { get; set; }

        private float _lastTime;

        public override bool KeyValue(string key, string value)
        {
            if (key == "framerate")
            {
                FrameRate = KeyValueUtils.ParseFloat(value);
                return true;
            }

            return base.KeyValue(key, value);
        }

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
