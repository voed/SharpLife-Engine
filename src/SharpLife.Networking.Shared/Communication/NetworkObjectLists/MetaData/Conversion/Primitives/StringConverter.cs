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

using Google.Protobuf;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives
{
    public sealed class StringConverter : BaseValueTypeConverter<string>
    {
        public static StringConverter Instance { get; } = new StringConverter();

        public override int MemberCount => 1;

        private StringConverter()
        {
        }

        public override bool Changed(object value, object previousValue)
        {
            if (value == null)
            {
                return previousValue != null;
            }

            return !value.Equals(previousValue);
        }

        public override void Write(object value, object previousValue, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);

            stream.WriteBool(value != null);

            if (value != null)
            {
                stream.WriteString((string)value);
            }
        }

        public override bool Read(CodedInputStream stream, object previousValue, out object result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                if (stream.ReadBool())
                {
                    result = stream.ReadString();
                }
                else
                {
                    result = null;
                }
            }
            else
            {
                result = null;
            }

            return changed;
        }
    }
}
