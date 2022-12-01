// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Internal;

namespace MQTTnet.Formatter
{
    public sealed class MqttBufferReader
    {
        byte[] _buffer = EmptyBuffer.Array;
        int _maxPosition;
        int _offset;

        public int BytesLeft => _maxPosition - _offset - Position;

        public bool EndOfStream => BytesLeft == 0;

        public int Position { get; private set; }

        public byte[] ReadBinaryData()
        {
            var length = ReadTwoByteInteger();

            if (length == 0)
            {
                return EmptyBuffer.Array;
            }

            ValidateReceiveBuffer(length);

            var result = new byte[length];
            Array.Copy(_buffer, Position + _offset, result, 0, length);
            Position += length;

            return result;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);

            return _buffer[_offset + Position++];
        }

        public uint ReadFourByteInteger()
        {
            ValidateReceiveBuffer(4);

#if NETCOREAPP3_0_OR_GREATER
            var value = System.Runtime.InteropServices.MemoryMarshal.Read<uint>(_buffer.AsSpan(_offset + Position));
            if (BitConverter.IsLittleEndian)
            {
                value = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(value);
            }
#else
            var byte0 = _buffer[_offset + Position];
            var byte1 = _buffer[_offset + Position + 1];
            var byte2 = _buffer[_offset + Position + 2];
            var byte3 = _buffer[_offset + Position + 3];

            var value = (uint)((byte0 << 24) | (byte1 << 16) | (byte2 << 8) | byte3);
#endif

            Position += 4;
            return value;
        }

        public byte[] ReadRemainingData()
        {
            var bufferLength = _maxPosition - Position - _offset;

            if (bufferLength == 0)
            {
                return EmptyBuffer.Array;
            }

            var buffer = new byte[bufferLength];
            Array.Copy(_buffer, Position + _offset, buffer, 0, bufferLength);

            Position += bufferLength;

            return buffer;
        }

        public string ReadString()
        {
            var length = ReadTwoByteInteger();

            if (length == 0)
            {
                return string.Empty;
            }

            ValidateReceiveBuffer(length);

#if NETCOREAPP3_0_OR_GREATER
            // AsSpan() version is slightly faster. Not much but at least a little bit.
            var result = Encoding.UTF8.GetString(_buffer.AsSpan(_offset + Position, length));
#else
            var result = Encoding.UTF8.GetString(_buffer, _offset + Position, length);
#endif            

            Position += length;
            return result;
        }

        public ushort ReadTwoByteInteger()
        {
            ValidateReceiveBuffer(2);

#if NETCOREAPP3_0_OR_GREATER
            var value = System.Runtime.InteropServices.MemoryMarshal.Read<ushort>(_buffer.AsSpan(_offset + Position));
            if (BitConverter.IsLittleEndian)
            {
                value = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(value);
            }
#else
            var msb = _buffer[_offset + Position];
            var lsb = _buffer[_offset + Position + 1];

            var value = (ushort)((msb << 8) | lsb);
#endif

            Position += 2;
            return value;
        }

        public uint ReadVariableByteInteger()
        {
            var multiplier = 1;
            var value = 0U;
            byte encodedByte;

            do
            {
                encodedByte = ReadByte();
                value += (uint)((encodedByte & 127) * multiplier);

                if (multiplier > 2097152)
                {
                    throw new MqttProtocolViolationException("Variable length integer is invalid.");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return value;
        }

        public void Seek(int position)
        {
            Position = position;
        }

        public void SetBuffer(byte[] buffer, int offset, int length)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _offset = offset;

            Position = 0;
            _maxPosition = offset + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateReceiveBuffer(int length)
        {
            var newPosition = Position + _offset + length;

            if (_maxPosition < newPosition)
            {
                throw new MqttProtocolViolationException($"Expected at least {Position + length} bytes but there are only {_maxPosition} bytes");
            }
        }
    }
}