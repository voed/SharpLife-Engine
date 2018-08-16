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
    public sealed class UInt8Converter : BaseArithmeticConverter<byte>
    {
        public static UInt8Converter Instance { get; } = new UInt8Converter();

        private UInt8Converter()
        {
        }

        public override void Write(in byte value, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);
            stream.WriteUInt32(value);
        }

        public override bool Read(CodedInputStream stream, out byte result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = (byte)stream.ReadUInt32();
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
