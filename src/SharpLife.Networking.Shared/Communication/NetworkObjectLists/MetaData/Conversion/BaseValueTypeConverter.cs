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

        public abstract bool Changed(object value, object previousValue);

        public abstract void Write(object value, CodedOutputStream stream);

        public abstract bool Read(CodedInputStream stream, out object result);
    }
}
