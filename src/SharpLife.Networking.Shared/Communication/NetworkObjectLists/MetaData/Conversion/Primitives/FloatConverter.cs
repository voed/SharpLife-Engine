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
    public sealed class FloatConverter : BasePrimitiveConverter<float>
    {
        public static FloatConverter Instance { get; } = new FloatConverter();

        private FloatConverter()
        {
        }

        public void Write(float value, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);
            stream.WriteFloat((float)value);
        }

        public override void Write(object value, CodedOutputStream stream)
        {
            Write((float)value, stream);
        }

        public bool Read(CodedInputStream stream, out float result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = stream.ReadFloat();
            }
            else
            {
                result = default;
            }

            return changed;
        }

        public override bool Read(CodedInputStream stream, out object result)
        {
            var changed = Read(stream, out var floatValue);

            result = floatValue;

            return changed;
        }
    }
}
