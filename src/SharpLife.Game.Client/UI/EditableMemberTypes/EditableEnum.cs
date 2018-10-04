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
        private sealed class EditableEnumSingleValue : IEditableMemberType
        {
            private readonly string _label;

            private readonly MemberInfo _info;

            private readonly Type _type;

            private string _value;

            public EditableEnumSingleValue(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
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

        private sealed class EditableEnumFlags : IEditableMemberType
        {
            private readonly string _label;

            private readonly MemberInfo _info;

            private readonly Type _type;

            //List of all values
            //Name, value and whether it's set
            private readonly (string Name, object Value, bool Checked)[] _values;

            private object _currentValue;

            public EditableEnumFlags(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
            {
                _label = $"{index}: {info.Name}";

                _info = info;
                _type = type;

                var names = _type.GetEnumNames();

                _values = new (string, object, bool)[names.Length];

                _currentValue = objectAccessor[_info.Name];

                for (var i = 0; i < names.Length; ++i)
                {
                    _values[i].Name = names[i];

                    _values[i].Value = Enum.Parse(_type, names[i]);

                    _values[i].Checked = GetEnumFlagState(_currentValue, _values[i].Value);
                }
            }

            public void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
            {
            }

            public void Display(object editObject, ObjectAccessor objectAccessor)
            {
                if (ImGui.CollapsingHeader(_label, TreeNodeFlags.NoTreePushOnOpen))
                {
                    _currentValue = objectAccessor[_info.Name];

                    UpdateCurrentValue();

                    var value = _currentValue;

                    for (var i = 0; i < _values.Length; ++i)
                    {
                        if (ImGui.Checkbox(_values[i].Name, ref _values[i].Checked))
                        {
                            SetEnumFlag(ref value, _values[i].Value, _values[i].Checked);
                        }
                    }

                    if (!value.Equals(_currentValue))
                    {
                        _currentValue = value;
                        objectAccessor[_info.Name] = _currentValue;
                    }
                }
            }

            private void UpdateCurrentValue()
            {
                for (var i = 0; i < _values.Length; ++i)
                {
                    _values[i].Checked = GetEnumFlagState(_currentValue, _values[i].Value);
                }
            }

            private bool GetEnumFlagState(object value, object valueToCheck)
            {
                //Special case for value 0
                if (0.Equals((int)valueToCheck))
                {
                    return 0.Equals((int)value);
                }

                switch (Type.GetTypeCode(_type.GetEnumUnderlyingType()))
                {
                    case TypeCode.Byte: return (((byte)value) & ((byte)valueToCheck)) != 0;

                    case TypeCode.SByte: return (((sbyte)value) & ((sbyte)valueToCheck)) != 0;

                    case TypeCode.Int16: return (((short)value) & ((short)valueToCheck)) != 0;

                    case TypeCode.UInt16: return (((ushort)value) & ((ushort)valueToCheck)) != 0;

                    case TypeCode.Int32: return (((int)value) & ((int)valueToCheck)) != 0;

                    case TypeCode.UInt32: return (((uint)value) & ((uint)valueToCheck)) != 0;

                    case TypeCode.Int64: return (((long)value) & ((long)valueToCheck)) != 0;

                    case TypeCode.UInt64: return (((ulong)value) & ((ulong)valueToCheck)) != 0;

                    default: throw new InvalidOperationException("Unsupported enum type");
                }
            }

            private void SetEnumFlag(ref object value, object valueToSet, bool state)
            {
                switch (Type.GetTypeCode(_type.GetEnumUnderlyingType()))
                {
                    case TypeCode.Byte:
                        {
                            if (state)
                            {
                                value = ((byte)value) | ((byte)valueToSet);
                            }
                            else
                            {
                                value = ((byte)value) & ~((byte)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.SByte:
                        {
                            if (state)
                            {
                                value = ((sbyte)value) | ((sbyte)valueToSet);
                            }
                            else
                            {
                                value = ((sbyte)value) & ~((sbyte)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.Int16:
                        {
                            if (state)
                            {
                                value = ((short)value) | ((short)valueToSet);
                            }
                            else
                            {
                                value = ((short)value) & ~((short)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.UInt16:
                        {
                            if (state)
                            {
                                value = ((ushort)value) | ((ushort)valueToSet);
                            }
                            else
                            {
                                value = ((ushort)value) & ~((ushort)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.Int32:
                        {
                            if (state)
                            {
                                value = ((int)value) | ((int)valueToSet);
                            }
                            else
                            {
                                value = ((int)value) & ~((int)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.UInt32:
                        {
                            if (state)
                            {
                                value = ((uint)value) | ((uint)valueToSet);
                            }
                            else
                            {
                                value = ((uint)value) & ~((uint)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.Int64:
                        {
                            if (state)
                            {
                                value = ((long)value) | ((long)valueToSet);
                            }
                            else
                            {
                                value = ((long)value) & ~((long)valueToSet);
                            }
                            break;
                        }

                    case TypeCode.UInt64:
                        {
                            if (state)
                            {
                                value = ((ulong)value) | ((ulong)valueToSet);
                            }
                            else
                            {
                                value = ((ulong)value) & ~((ulong)valueToSet);
                            }
                            break;
                        }
                }
            }
        }

        private readonly IEditableMemberType _display;

        public EditableEnum(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
        {
            //TODO: this can be handled in the factory function
            if (type.GetCustomAttribute<FlagsAttribute>() != null)
            {
                _display = new EditableEnumFlags(index, editObject, info, type, objectAccessor);
            }
            else
            {
                _display = new EditableEnumSingleValue(index, editObject, info, type, objectAccessor);
            }
        }

        public void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
        {
            _display.Initialize(index, editObject, info, objectAccessor);
        }

        public void Display(object editObject, ObjectAccessor objectAccessor)
        {
            _display.Display(editObject, objectAccessor);
        }
    }
}
