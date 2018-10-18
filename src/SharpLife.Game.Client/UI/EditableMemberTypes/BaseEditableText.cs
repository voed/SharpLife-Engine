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
using System.Text;

namespace SharpLife.Game.Client.UI.EditableMemberTypes
{
    public abstract class BaseEditableText : IEditableMemberType
    {
        private readonly string _label;

        protected readonly MemberInfo _info;

        private readonly byte[] _buffer = new byte[256];

        private readonly InputTextFlags _flags;

        protected BaseEditableText(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor, InputTextFlags flags)
        {
            _label = $"{index}: {info.Name}";

            _info = info;

            _flags = flags;
        }

        public abstract void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor);

        public void Display(object editObject, ObjectAccessor objectAccessor)
        {
            if (ImGui.InputText(_label, _buffer, (uint)_buffer.Length, _flags | InputTextFlags.EnterReturnsTrue, null))
            {
                var text = Encoding.UTF8.GetString(_buffer);

                OnValueChanged(objectAccessor, text);
            }
        }

        protected void SetValue(string value)
        {
            if (value != null)
            {
                Encoding.UTF8.GetBytes(value, new Span<byte>(_buffer));
            }
            else
            {
                Array.Clear(_buffer, 0, _buffer.Length);
            }
        }

        protected abstract void OnValueChanged(ObjectAccessor objectAccessor, string newValue);
    }
}
