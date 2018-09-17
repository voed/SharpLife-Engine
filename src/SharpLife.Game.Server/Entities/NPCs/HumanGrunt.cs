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
using SharpLife.Models.Studio;

namespace SharpLife.Game.Server.Entities.NPCs
{
    [LinkEntityToClass("monster_human_grunt")]
    [Networkable(UseBaseType = true)]
    public class HumanGrunt : NetworkedEntity
    {
        public override void Precache()
        {
            Model = Context.EngineModels.LoadModel("models/hgrunt.mdl");

            base.Precache();
        }

        protected override void Spawn()
        {
            Precache();

            FrameRate = 1;
        }

        private float _lastTime;

        private float m_flAnimTime;

        private float m_flLastEventCheck;

        //TODO: implement

        public override void Think()
        {
            if (Model is StudioModel studioModel)
            {
                float dt = 0;
                float flMax = 0.1f;

                if (dt == 0.0)
                {
                    dt = ((float)(Context.Time.ElapsedTime - m_flAnimTime));
                    if (dt <= 0.001)
                    {
                        m_flAnimTime = (float)Context.Time.ElapsedTime;
                        return;
                    }
                }

                if (m_flAnimTime == 0)
                    dt = 0.0f;

                if (flMax != -1.0f)
                {
                    if (dt > flMax)
                        dt = flMax;
                }

                var sequence = studioModel.StudioFile.Sequences[0];

                var increment = (float)(FrameRate * sequence.FPS * dt);

                Frame += increment;

                _lastTime = (float)Context.Time.ElapsedTime;

                if (sequence.FrameCount <= 1)
                {
                    Frame = 0;
                }
                else
                {
                    float flOldFrame = Frame;

                    // wrap
                    Frame -= (int)(Frame / (sequence.FrameCount - 1)) * (sequence.FrameCount - 1);

                    //Wrapped
                    if (flOldFrame > Frame)
                    {
                        m_flLastEventCheck = Frame - increment;
                    }
                }

                m_flAnimTime = (float)Context.Time.ElapsedTime;
            }
        }
    }
}
