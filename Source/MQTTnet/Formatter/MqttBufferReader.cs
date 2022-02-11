// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Implementations;

namespace MQTTnet.Formatter
{
    public sealed class MqttBufferReader
    {
        byte[] _buffer = PlatformAbstractionLayer.EmptyByteArray;
        int _initialOffset;
        int _length;

        public bool EndOfStream => Offset == _length;

        public int Length => _length - Offset;

        public int Offset { get; private set; }

        public byte[] ReadBinaryData()
        {
            var length = ReadTwoByteInteger();

            ValidateReceiveBuffer(length);

            var result = new byte[length];
            Array.Copy(_buffer, Offset, result, 0, length);
            Offset += length;

            return result;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);

            return _buffer[Offset++];
        }

        public uint ReadFourByteInteger()
        {
            ValidateReceiveBuffer(4);

            var byte0 = _buffer[Offset++];
            var byte1 = _buffer[Offset++];
            var byte2 = _buffer[Offset++];
            var byte3 = _buffer[Offset++];

            return (uint)((byte0 << 24) | (byte1 << 16) | (byte2 << 8) | byte3);
        }

        public byte[] ReadRemainingData()
        {
            var bufferLength = _length - Offset;
            var buffer = new byte[bufferLength];
            Array.Copy(_buffer, Offset, buffer, 0, bufferLength);

            return buffer;
        }

        public string ReadString()
        {
            var length = ReadTwoByteInteger();

            ValidateReceiveBuffer(length);

            var result = Encoding.UTF8.GetString(_buffer, Offset, length);
            Offset += length;
            return result;
        }

        public ushort ReadTwoByteInteger()
        {
            ValidateReceiveBuffer(2);

            var msb = _buffer[Offset++];
            var lsb = _buffer[Offset++];

            return (ushort)((msb << 8) | lsb);
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
            Offset = _initialOffset + position;
        }

        public void SetBuffer(byte[] buffer, int offset, int length)
        {
            _buffer = buffer;
            _initialOffset = offset;
            Offset = offset;
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateReceiveBuffer(int length)
        {
            if (_length < Offset + length)
            {
                throw new MqttProtocolViolationException($"Expected at least {Offset + length} bytes but there are only {_length} bytes");
            }
        }
    }
}