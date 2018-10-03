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
    public sealed class DoubleConverter : BasePrimitiveConverter<double>
    {
        public static DoubleConverter Instance { get; } = new DoubleConverter();

        private DoubleConverter()
        {
        }

        public override void Write(object value, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);
            stream.WriteDouble((double)value);
        }

        public override bool Read(CodedInputStream stream, out object result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = stream.ReadDouble();
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
