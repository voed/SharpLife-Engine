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
using System.Reflection;

namespace SharpLife.Game.Client.UI.EditableMemberTypes
{
    public sealed class EditableBoolean : IEditableMemberType
    {
        private readonly string _label;

        private readonly MemberInfo _info;

        private bool _value;

        public EditableBoolean(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
        {
            _label = $"{index}: {info.Name}";

            _info = info;

            _value = (bool)objectAccessor[info.Name];
        }

        public void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
        {
        }

        public void Display(object editObject, ObjectAccessor objectAccessor)
        {
            if (ImGui.Checkbox(_label, ref _value))
            {
                objectAccessor[_info.Name] = _value;
            }
        }
    }
}
