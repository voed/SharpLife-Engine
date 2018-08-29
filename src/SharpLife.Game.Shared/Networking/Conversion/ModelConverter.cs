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
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Models;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists.MetaData.Conversion.Primitives;
using System;

namespace SharpLife.Game.Shared.Networking.Conversion
{
    /// <summary>
    /// Converter for IModel instances
    /// Stored as a <see cref="ModelIndex"/>
    /// </summary>
    public sealed class ModelConverter : ITypeConverter
    {
        private readonly IEngineModels _engineModels;

        public object Default => null;

        public int MemberCount => 1;

        public ModelConverter(IEngineModels engineModels)
        {
            _engineModels = engineModels ?? throw new ArgumentNullException(nameof(engineModels));
        }

        public object Copy(object value)
        {
            return value != null ? _engineModels.IndexOf((IModel)value) : new ModelIndex();
        }

        public object CreateInstance(Type targetType, object value)
        {
            var index = (ModelIndex)value;

            return _engineModels.GetModel(index);
        }

        public bool EncodeAndWrite(object value, object previousValue, CodedOutputStream stream)
        {
            return Int32Converter.Instance.EncodeAndWrite(((ModelIndex)value).Index, ((ModelIndex)previousValue).Index, stream);
        }

        public void Write(object value, CodedOutputStream stream)
        {
            Int32Converter.Instance.Write(((ModelIndex)value).Index, stream);
        }

        public bool ReadAndDecode(CodedInputStream stream, object previousValue, out object result)
        {
            var decodeResult = Int32Converter.Instance.ReadAndDecode(stream, ((ModelIndex)previousValue).Index, out var resultValue);

            result = new ModelIndex(resultValue);

            return decodeResult;
        }

        public bool Read(CodedInputStream stream, out object result)
        {
            var readResult = Int32Converter.Instance.Read(stream, out int resultValue);

            result = new ModelIndex(resultValue);

            return readResult;
        }
    }
}
