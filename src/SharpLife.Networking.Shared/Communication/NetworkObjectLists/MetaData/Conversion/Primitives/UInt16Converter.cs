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
    public sealed class UInt16Converter : BaseArithmeticConverter<ushort>
    {
        public static UInt16Converter Instance { get; } = new UInt16Converter();

        private UInt16Converter()
        {
        }

        public override void Write(in ushort value, CodedOutputStream stream)
        {
            ConversionUtils.AddChangedValue(stream);
            stream.WriteUInt32(value);
        }

        public override bool Read(CodedInputStream stream, out ushort result)
        {
            var changed = stream.ReadBool();

            if (changed)
            {
                result = (ushort)stream.ReadUInt32();
            }
            else
            {
                result = default;
            }

            return changed;
        }
    }
}
