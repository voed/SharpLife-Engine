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
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;
using System.Numerics;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion
{
    public sealed class Vector2Converter : BaseValueTypeConverter<Vector2>
    {
        public static Vector2Converter Instance { get; } = new Vector2Converter();

        public override int MemberCount => 2 * FloatConverter.Instance.MemberCount;

        private Vector2Converter()
        {
        }

        public override bool Encode(in Vector2 value, in Vector2 previousValue, out Vector2 result)
        {
            var xDiff = FloatConverter.Instance.Encode(value.X, previousValue.X, out var x);
            var yDiff = FloatConverter.Instance.Encode(value.Y, previousValue.Y, out var y);

            result = new Vector2(x, y);

            return xDiff || yDiff;
        }

        //Overridden because it is more efficient to ues FloatConverter's EncodeAndWrite directly
        public override bool EncodeAndWrite(in Vector2 value, in Vector2 previousValue, CodedOutputStream stream)
        {
            var xDiff = FloatConverter.Instance.EncodeAndWrite(value.X, previousValue.X, stream);
            var yDiff = FloatConverter.Instance.EncodeAndWrite(value.Y, previousValue.Y, stream);

            return xDiff || yDiff;
        }

        public override void Write(in Vector2 value, CodedOutputStream stream)
        {
            if (value.X != 0.0f)
            {
                FloatConverter.Instance.Write(value.X, stream);
            }
            else
            {
                ConversionUtils.AddUnchangedValue(stream);
            }

            if (value.Y != 0.0f)
            {
                FloatConverter.Instance.Write(value.Y, stream);
            }
            else
            {
                ConversionUtils.AddUnchangedValue(stream);
            }
        }

        public override void Decode(in Vector2 value, in Vector2 previousValue, out Vector2 result)
        {
            //Already decoded by Read
            result = value;
        }

        public override bool Read(CodedInputStream stream, out Vector2 result)
        {
            var xDiff = FloatConverter.Instance.Read(stream, out float x);
            var yDiff = FloatConverter.Instance.Read(stream, out float y);

            result = new Vector2(x, y);

            return xDiff || yDiff;
        }
    }
}
