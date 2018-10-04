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
    public sealed class EditableEnum : IEditableMemberType
    {
        private readonly string _label;

        private readonly MemberInfo _info;

        private readonly Type _type;

        private string _value;

        public EditableEnum(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
        {
            _label = $"{index}: {info.Name}";

            _info = info;
            _type = type;

            _value = _type.GetEnumName(objectAccessor[info.Name]);
        }

        public void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
        {
        }

        public void Display(object editObject, ObjectAccessor objectAccessor)
        {
            //TODO: detect Flags type enums and provide a bit vector editor
            if (ImGui.BeginCombo(_label, _value, ComboFlags.HeightRegular))
            {
                foreach (var enumValue in _type.GetEnumNames())
                {
                    var isSelected = _value == enumValue;

                    if (ImGui.Selectable(enumValue, isSelected))
                    {
                        if (Enum.TryParse(_type, enumValue, out var underlyingValue))
                        {
                            objectAccessor[_info.Name] = underlyingValue;
                            _value = enumValue;
                        }
                        else
                        {
                            _value = _type.GetEnumName(objectAccessor[_info.Name]);
                        }
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }
        }
    }
}
