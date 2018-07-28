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

using ImGuiNET;
using SharpLife.Engine.API.Engine.Client;
using SharpLife.Engine.API.Game.Client;
using SharpLife.Utility;
using System;

namespace SharpLife.Game.Client.UI
{
    public sealed class ImGuiInterface : IClientUI
    {
        private readonly FrameTimeAverager _fta = new FrameTimeAverager(0.666);

        private readonly IViewState _viewState;

        public ImGuiInterface(IViewState viewState)
        {
            _viewState = viewState ?? throw new ArgumentNullException(nameof(viewState));
        }

        public void Update(float deltaSeconds)
        {
            _fta.AddTime(deltaSeconds);
        }

        public void Draw()
        {
            if (ImGui.BeginMainMenuBar())
            {
                ImGui.Text(_fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms"));

                ImGui.TextUnformatted($"Camera Position: {_viewState.Origin} Camera Angles: Pitch {_viewState.Angles.X} Yaw {_viewState.Angles.Y}");

                ImGui.EndMainMenuBar();
            }
        }
    }
}
