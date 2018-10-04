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

using FastMember;
using ImGuiNET;
using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpLife.Game.Client.UI.EditableMemberTypes.Vector3DisplayFormats
{
    public sealed class Vector3Normal : IVector3Display
    {
        private readonly string[] _labels;

        private readonly MemberInfo _info;

        private Vector3 _value;

        public Vector3Normal(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
        {
            _labels = new string[] {
                $"{index}: {info.Name}.X",
                $"{index}: {info.Name}.Y",
                $"{index}: {info.Name}.Z"
            };

            _info = info;

            _value = (Vector3)objectAccessor[info.Name];
        }

        public void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
        {
        }

        public unsafe void Display(object editObject, ObjectAccessor objectAccessor)
        {
            var pVector = (float*)Unsafe.AsPointer(ref _value);

            var changed = false;

            for (var i = 0; i < 3; ++i)
            {
                if (ImGui.SliderFloat(_labels[i], ref pVector[i], -1, 1, "%.3f", 1))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                objectAccessor[_info.Name] = _value;
            }
        }
    }
}
