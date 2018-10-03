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
    public sealed class UInt32Converter : BasePrimitiveConverter<uint>
    {
        public static UInt32Converter Instance { get; } = new UInt32Converter();

        private UInt32Converter()
        {
        }

        public override void Write(object value, object previousValue, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);
            stream.WriteUInt32((uint)value);
        }

        public override bool Read(CodedInputStream stream, object previousValue, out object result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = stream.ReadUInt32();
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
