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
    /// Provides methods to operate on a type to convert it into various forms needed for networking
    /// </summary>
    public interface ITypeConverter
    {
        object Default { get; }

        /// <summary>
        /// The number of members that a type adds to the object update
        /// </summary>
        int MemberCount { get; }

        /// <summary>
        /// Creates a copy of the given value
        /// null should always return null
        /// This need not return another instance of the same type,
        /// if the type contains a lot of non-networked variables it may return a different type,
        /// or even an array of objects
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        object Copy(object value);

        /// <summary>
        /// Creates an instance of the original type from a snapshot value
        /// </summary>
        /// <param name="targetType">The static type of the member</param>
        /// <param name="value"></param>
        /// <returns></returns>
        object CreateInstance(Type targetType, object value);

        /// <summary>
        /// Returns whether the value has changed compared to the previous value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        /// <returns></returns>
        bool Changed(object value, object previousValue);

        /// <summary>
        /// Write a value to the stream
        /// </summary>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        void Write(object value, object previousValue, CodedOutputStream stream);

        /// <summary>
        /// Reads a value from the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="previousValue"></param>
        /// <param name="result"></param>
        /// <returns>Whether the stream contained a value or not</returns>
        bool Read(CodedInputStream stream, object previousValue, out object result);
    }
}
