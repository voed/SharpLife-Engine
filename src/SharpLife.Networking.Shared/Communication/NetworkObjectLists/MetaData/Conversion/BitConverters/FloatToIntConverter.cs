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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.BitConverters
{
    /// <summary>
    /// Converts floats to integers for networking
    /// No delta calculations between values is performed to avoid loss of accuracy due to precision issues
    /// TODO: implement bit stream so that bit operations can be performed
    /// TODO: converters should be checked for compatibility so unmatched ones don't break
    /// </summary>
    public class FloatToIntConverter : BaseValueTypeConverter<float>
    {
        public readonly BitConverterOptions Options;

        public override int MemberCount => 1;

        public FloatToIntConverter(in BitConverterOptions options)
        {
            Options = options;
        }

        public override bool Encode(in float value, in float previousValue, out float result)
        {
            result = value;

            return value != previousValue;
        }

        public override void Write(in float value, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);

            var isNegative = value < 0;

            var result = value;

            if (isNegative)
            {
                result = -result;
            }

            if (Options.ShouldMultiply)
            {
                result *= Options.Multiplier;
            }

            stream.WriteInt32((int)result);

            if ((Options.Flags & BitConverterFlags.Signed) != 0)
            {
                stream.WriteBool(isNegative);
            }
        }

        public override void Decode(in float value, in float previousValue, out float result)
        {
            result = value;
        }

        public override bool Read(CodedInputStream stream, out float result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = stream.ReadInt32();

                if (Options.ShouldMultiply)
                {
                    result /= Options.Multiplier;
                }

                if (Options.ShouldPostMultiply)
                {
                    result *= Options.PostMultiplier;
                }

                if ((Options.Flags & BitConverterFlags.Signed) != 0)
                {
                    var isNegative = stream.ReadBool();

                    if (isNegative)
                    {
                        result = -result;
                    }
                }
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
