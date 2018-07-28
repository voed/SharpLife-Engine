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

using Lidgren.Network;
using System;
using System.IO;

namespace SharpLife.Networking.Shared
{
    /// <summary>
    /// Stream wrapper around a <see cref="NetBuffer"/>
    /// TODO: test this to make sure it works as it should
    /// </summary>
    public sealed class NetBufferStream : Stream
    {
        private readonly NetBuffer _buffer;

        public NetBufferStream(NetBuffer buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public override void Flush()
        {
            //Nothing
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesAvailable = _buffer.LengthBytes - _buffer.PositionInBytes;

            if (bytesAvailable < count)
            {
                count = bytesAvailable;
            }

            _buffer.ReadBytes(buffer, offset, count);

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _buffer.Position = offset * 8;
                    break;
                case SeekOrigin.Current:
                    _buffer.Position += offset * 8;
                    break;
                case SeekOrigin.End:
                    _buffer.Position = (_buffer.LengthBytes - offset) * 8;
                    break;
            }

            return _buffer.PositionInBytes;
        }

        public override void SetLength(long value)
        {
            _buffer.LengthBytes = (int)value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Write(buffer, offset, count);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _buffer.LengthBytes;

        public override long Position
        {
            get => _buffer.PositionInBytes;
            set => _buffer.Position = value * 8;
        }
    }
}
