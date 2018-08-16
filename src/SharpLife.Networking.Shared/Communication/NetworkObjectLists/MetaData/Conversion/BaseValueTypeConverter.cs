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
using System;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion
{
    /// <summary>
    /// Base converter class for value types to implement common behavior
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseValueTypeConverter<T> : ITypeConverter
    {
        public object Default { get; } = default(T);

        public abstract int MemberCount { get; }

        public object Copy(object value) => value;

        //Value types can just return the snapshot instance
        public object CreateInstance(Type targetType, object value) => value;

        /// <summary>
        /// Encodes a value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public abstract bool Encode(in T value, in T previousValue, out T result);

        public virtual bool EncodeAndWrite(in T value, in T previousValue, CodedOutputStream stream)
        {
            if (Encode(value, previousValue, out var result))
            {
                Write(result, stream);
                return true;
            }

            ConversionUtils.AddUnchangedValue(stream);

            return false;
        }

        public bool EncodeAndWrite(object value, object previousValue, CodedOutputStream stream)
        {
            return EncodeAndWrite((T)value, (T)previousValue, stream);
        }

        public abstract void Write(in T value, CodedOutputStream stream);

        public void Write(object value, CodedOutputStream stream)
        {
            Write((T)value, stream);
        }

        public abstract void Decode(in T value, in T previousValue, out T result);

        public bool ReadAndDecode(CodedInputStream stream, object previousValue, out object result)
        {
            if (Read(stream, out T resultValue))
            {
                Decode(resultValue, (T)previousValue, out resultValue);

                result = resultValue;

                return true;
            }

            result = null;

            return false;
        }

        public abstract bool Read(CodedInputStream stream, out T result);

        public bool Read(CodedInputStream stream, out object result)
        {
            var returnValue = Read(stream, out T resultValue);

            result = resultValue;

            return returnValue;
        }
    }
}
