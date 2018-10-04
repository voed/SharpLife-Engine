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
using SharpLife.Game.Client.UI.EditableMemberTypes.Vector3DisplayFormats;
using SharpLife.Game.Shared.UI.EditableMemberTypes;
using System;
using System.Reflection;

namespace SharpLife.Game.Client.UI.EditableMemberTypes
{
    public sealed class EditableVector3 : IEditableMemberType
    {
        private readonly IVector3Display _display;

        public EditableVector3(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
        {
            var editorAttr = info.GetCustomAttribute<ObjectEditorVector3Attribute>();

            switch (editorAttr?.DisplayFormat ?? Vector3DisplayFormat.Floats)
            {
                case Vector3DisplayFormat.Floats:
                    _display = new Vector3Floats(index, editObject, info, type, objectAccessor);
                    break;

                case Vector3DisplayFormat.Color24:
                    _display = new Vector3Color24(index, editObject, info, type, objectAccessor, true);
                    break;

                case Vector3DisplayFormat.ColorFloat:
                    _display = new Vector3Color24(index, editObject, info, type, objectAccessor, false);
                    break;

                case Vector3DisplayFormat.Normal:
                    _display = new Vector3Normal(index, editObject, info, type, objectAccessor);
                    break;

                case Vector3DisplayFormat.AnglesDegrees:
                    _display = new Vector3AnglesDegrees(index, editObject, info, type, objectAccessor);
                    break;

                case Vector3DisplayFormat.AnglesRadians:
                    _display = new Vector3AnglesRadians(index, editObject, info, type, objectAccessor);
                    break;

                default: throw new InvalidOperationException("Unknown Vector3 display format");
            }
        }

        public void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
        {
            _display.Initialize(index, editObject, info, objectAccessor);
        }

        public unsafe void Display(object editObject, ObjectAccessor objectAccessor)
        {
            _display.Display(editObject, objectAccessor);
        }
    }
}
