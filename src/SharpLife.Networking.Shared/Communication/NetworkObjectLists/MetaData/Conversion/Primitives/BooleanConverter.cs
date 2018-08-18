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
    public sealed class BooleanConverter : BaseValueTypeConverter<bool>
    {
        public static BooleanConverter Instance { get; } = new BooleanConverter();

        public override int MemberCount => 1;

        private BooleanConverter()
        {
        }

        public override bool Encode(in bool value, in bool previousValue, out bool result)
        {
            result = value;

            return value != previousValue;
        }

        public override void Write(in bool value, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);
            stream.WriteBool(value);
        }

        public override void Decode(in bool value, in bool previousValue, out bool result)
        {
            result = value;
        }

        public override bool Read(CodedInputStream stream, out bool result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = stream.ReadBool();
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
