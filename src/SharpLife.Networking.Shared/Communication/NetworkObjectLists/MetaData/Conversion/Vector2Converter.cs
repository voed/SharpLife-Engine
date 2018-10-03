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

        public override bool Changed(object value, object previousValue)
        {
            return !value.Equals(previousValue);
        }

        public override void Write(object value, object previousValue, CodedOutputStream stream)
        {
            var vector = (Vector2)value;

            FloatConverter.Instance.Write(vector.X, stream);
            FloatConverter.Instance.Write(vector.Y, stream);
        }

        public override bool Read(CodedInputStream stream, object previousValue, out object result)
        {
            var xDiff = FloatConverter.Instance.Read(stream, out float x);
            var yDiff = FloatConverter.Instance.Read(stream, out float y);

            result = new Vector2(x, y);

            return xDiff || yDiff;
        }
    }
}
