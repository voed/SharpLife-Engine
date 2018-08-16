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
using System.Collections.Generic;

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion
{
    /// <summary>
    /// Converter for <see cref="IList{T}"/>
    /// Include support for arrays
    /// </summary>
    /// <typeparam name="T">The type that the list stores</typeparam>
    public sealed class ListConverter<T> : ITypeConverter
    {
        private readonly ITypeConverter _typeConverter;

        public object Default => null;

        //Lists add everything as a child object to ensure the variable length doesn't cause problems
        public int MemberCount => 1;

        public ListConverter(ITypeConverter typeConverter)
        {
            _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
        }

        public object Copy(object value)
        {
            var list = (IList<T>)value;

            if (list == null)
            {
                return null;
            }

            var copy = new T[list.Count];

            for (var i = 0; i < list.Count; ++i)
            {
                copy[i] = (T)_typeConverter.Copy(list[i]);
            }

            return copy;
        }

        public object CreateInstance(Type targetType, object value)
        {
            if (value == null)
            {
                return null;
            }

            //If the static type is an interface, provide an array as the instance
            //TODO: provide a way to construct the exact type
            if (targetType.IsInterface)
            {
                return Copy(value);
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var original = (T[])value;

                var list = new List<T>(original.Length);

                foreach (var element in original)
                {
                    list.Add((T)_typeConverter.Copy(element));
                }

                return list;
            }

            throw new InvalidOperationException();
        }

        public bool Write(T[] list, T[] previousList, bool isDelta, CodedOutputStream stream)
        {
            //null list to null list is no change
            //Full updates can end up here, so make sure we send a null explicitly
            if (list == null && previousList == null && isDelta)
            {
                ConversionUtils.AddUnchangedValue(stream);
                return false;
            }

            ConversionUtils.AddChangedValue(stream);

            //First boolean indicates whether the list is null or not
            stream.WriteBool(list != null);

            if (list != null)
            {
                //If the number of elements remains the same, only send changes between elements
                //Otherwise, resend the entire list
                var resendEntireList = previousList == null || list.Length != previousList.Length;

                //Second boolean indicates whether the entire list is being sent
                stream.WriteBool(resendEntireList);

                //Count is number of values - 2
                if (resendEntireList)
                {
                    stream.WriteInt32(list.Length);

                    //The receiver will not know the capacity that the transmitter has, this shouldn't really matter unless the list changes size often
                    foreach (var element in list)
                    {
                        _typeConverter.Write(element, stream);
                    }
                }
                else
                {
                    //var changes = false;

                    for (var i = 0; i < list.Length; ++i)
                    {
                        //Each element starts with a true
                        stream.WriteBool(true);

                        /*var changedElement = */
                        _typeConverter.EncodeAndWrite(list[i], previousList[i], stream);

                        //changes = changes || changedElement;
                    }

                    //List ends with false to denote end of the list
                    stream.WriteBool(false);

                    //TODO: figure out if this can be done with coded streams
                    //If we were sending a delta and nothing changed, ignore
                    //if (!changes)
                    //{
                    //    ConversionUtils.AddUnchangedValue(stream);
                    //    return false;
                    //}
                }
            }

            return true;
        }

        public bool EncodeAndWrite(object value, object previousValue, CodedOutputStream stream)
        {
            //This operates on the copied list, which is an array
            var list = (T[])value;
            var previousList = (T[])previousValue;

            return Write(list, previousList, true, stream);
        }

        public void Write(object value, CodedOutputStream stream)
        {
            //This operates on the copied list, which is an array
            var list = (T[])value;

            Write(list, null, false, stream);
        }

        public bool ReadAndDecode(CodedInputStream stream, object previousValue, out object result)
        {
            var previousList = (T[])previousValue;

            if (!stream.ReadBool())
            {
                result = null;
                return false;
            }

            //List is null
            if (!stream.ReadBool())
            {
                result = null;
                return true;
            }

            //Is it an entire list?
            if (stream.ReadBool())
            {
                var count = stream.ReadInt32();

                var list = new T[count];

                for (var index = 0; index < count; ++index)
                {
                    _typeConverter.Read(stream, out var element);

                    list[index] = (T)element;
                }

                result = list;
            }
            else
            {
                var index = 0;

                var list = new List<T>();

                while (stream.ReadBool())
                {
                    if (_typeConverter.ReadAndDecode(stream, previousList != null ? previousList[index] : default, out var element))
                    {
                        list.Add((T)element);
                    }
                    else
                    {
                        list.Add(previousList != null ? previousList[index] : default);
                    }

                    ++index;
                }

                result = list.ToArray();
            }

            return true;
        }

        public bool Read(CodedInputStream stream, out object result)
        {
            return ReadAndDecode(stream, null, out result);
        }
    }
}
