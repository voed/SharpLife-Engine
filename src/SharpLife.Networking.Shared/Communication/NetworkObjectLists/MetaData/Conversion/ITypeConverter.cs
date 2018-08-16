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
        /// or even an array of jbects
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
        /// Delta encode and write a value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        /// <param name="stream"></param>
        /// <returns>Whether the value was written</returns>
        bool EncodeAndWrite(object value, object previousValue, CodedOutputStream stream);

        /// <summary>
        /// Write a full value
        /// This does not use delta encoding and will always write the state of the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        void Write(object value, CodedOutputStream stream);

        /// <summary>
        /// Reads and delta decodes a value
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="previousValue"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool ReadAndDecode(CodedInputStream stream, object previousValue, out object result);

        /// <summary>
        /// Reads a value
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="result"></param>
        /// <returns>Whether the stream contained a change or not</returns>
        bool Read(CodedInputStream stream, out object result);
    }
}
