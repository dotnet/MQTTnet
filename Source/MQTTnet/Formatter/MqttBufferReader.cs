// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Implementations;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttBufferReader
    {
        byte[] _buffer = PlatformAbstractionLayer.EmptyByteArray;
        int _initialPosition;
        int _length;
        int _position;

        public bool EndOfStream => _position == _length;
        
        public int Position => _position;
        
        public int BytesLeft => _length - _position;

        public byte[] ReadBinaryData()
        {
            var length = ReadTwoByteInteger();

            if (length == 0)
            {
                return PlatformAbstractionLayer.EmptyByteArray;
            }

            ValidateReceiveBuffer(length);

            var result = new byte[length];
            Array.Copy(_buffer, _position, result, 0, length);
            _position += length;

            return result;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);

            return _buffer[_position++];
        }

        public uint ReadFourByteInteger()
        {
            ValidateReceiveBuffer(4);

            var byte0 = _buffer[_position++];
            var byte1 = _buffer[_position++];
            var byte2 = _buffer[_position++];
            var byte3 = _buffer[_position++];

            return (uint)((byte0 << 24) | (byte1 << 16) | (byte2 << 8) | byte3);
        }

        public byte[] ReadRemainingData()
        {
            var bufferLength = _length - _position;

            if (bufferLength == 0)
            {
                return PlatformAbstractionLayer.EmptyByteArray;
            }
            
            var buffer = new byte[bufferLength];
            Array.Copy(_buffer, _position, buffer, 0, bufferLength);

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

            var result = Encoding.UTF8.GetString(_buffer, _position, length);
            _position += length;
            return result;
        }

        public ushort ReadTwoByteInteger()
        {
            ValidateReceiveBuffer(2);

            var msb = _buffer[_position++];
            var lsb = _buffer[_position++];

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
            _position = _initialPosition + position;
        }

        public void SetBuffer(byte[] buffer, int position, int length)
        {
            _buffer = buffer;
            _initialPosition = position;
            _position = position;
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateReceiveBuffer(int length)
        {
            if (_length < _position + length)
            {
                throw new MqttProtocolViolationException($"Expected at least {Position + length} bytes but there are only {_length} bytes");
            }
        }
    }
}