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
    /// <summary>
    /// Converts floats to integers for networking
    /// TODO: implement bit stream so that bit operations can be performed
    /// TODO: converters should be checked for compatibility so unmatched ones don't break
    /// </summary>
    public class FloatToIntConverter : BasePrimitiveConverter<float>
    {
        public override void Write(object value, object previousValue, in BitConverterOptions options, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);

            var floatValue = (float)value;

            var isNegative = floatValue < 0;

            var result = floatValue;

            if (isNegative)
            {
                result = -result;
            }

            if (options.ShouldMultiply)
            {
                result *= options.Multiplier;
            }

            stream.WriteInt32((int)result);

            if ((options.Flags & BitConverterFlags.Signed) != 0)
            {
                stream.WriteBool(isNegative);
            }
        }

        public override bool Read(CodedInputStream stream, object previousValue, in BitConverterOptions options, out object result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                var floatValue = (float)stream.ReadInt32();

                if (options.ShouldMultiply)
                {
                    floatValue /= options.Multiplier;
                }

                if (options.ShouldPostMultiply)
                {
                    floatValue *= options.PostMultiplier;
                }

                if ((options.Flags & BitConverterFlags.Signed) != 0)
                {
                    var isNegative = stream.ReadBool();

                    if (isNegative)
                    {
                        floatValue = -floatValue;
                    }
                }

                result = floatValue;
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
