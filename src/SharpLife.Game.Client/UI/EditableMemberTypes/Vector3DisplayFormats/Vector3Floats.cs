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
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpLife.Game.Client.UI.EditableMemberTypes.Vector3DisplayFormats
{
    public sealed class Vector3Floats : IVector3Display
    {
        private readonly string[] _labels;

        private readonly MemberInfo _info;

        private readonly byte[][] _buffers = new byte[3][] { new byte[256], new byte[256], new byte[256] };

        private Vector3 _value;

        public Vector3Floats(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
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
            SetValue(0, _value.X);
            SetValue(1, _value.Y);
            SetValue(2, _value.Z);
        }

        public unsafe void Display(object editObject, ObjectAccessor objectAccessor)
        {
            //TODO: use InputFloat3 when it's fixed

            var pVector = (float*)Unsafe.AsPointer(ref _value);

            var changed = false;

            for (var i = 0; i < 3; ++i)
            {
                var buffer = _buffers[i];

                if (ImGui.InputText(_labels[i], buffer, (uint)buffer.Length, InputTextFlags.CharsDecimal | InputTextFlags.EnterReturnsTrue, null))
                {
                    var text = Encoding.UTF8.GetString(buffer);

                    if (float.TryParse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var result) && pVector[i] != result)
                    {
                        pVector[i] = result;
                        changed = true;
                    }
                    else
                    {
                        SetValue(0, pVector[i]);
                    }
                }
            }

            if (changed)
            {
                objectAccessor[_info.Name] = _value;
            }
        }

        private void SetValue(int index, float value)
        {
            Encoding.UTF8.GetBytes(value.ToString(NumberFormatInfo.InvariantInfo), new Span<byte>(_buffers[index]));
        }
    }
}
