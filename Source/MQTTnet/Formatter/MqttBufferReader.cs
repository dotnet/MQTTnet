// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;


namespace MQTTnet.Formatter
{
    public sealed class MqttBufferReader
    {
        long _position = default;
        ReadOnlySequence<byte> _buffer = default;


        public long BytesLeft => _buffer.Length - _position;

        public bool EndOfStream => _buffer.Length <= _position;

        public long Position => _position;

        public ReadOnlySequence<byte> ReadBinaryData()
        {
            var length = ReadTwoByteInteger();
            if (length == 0)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            ValidateReceiveBuffer(length);
            var buffer = _buffer.Slice(_position, length);

            _position += length;
            return buffer;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);

            var reader = new SequenceReader<byte>(_buffer);
            reader.Advance(_position);
            reader.TryRead(out byte value);

            _position += 1;
            return value;
        }

        public ushort ReadTwoByteInteger()
        {
            ValidateReceiveBuffer(2);

            var reader = new SequenceReader<byte>(_buffer);
            reader.Advance(_position);
            reader.TryReadBigEndian(out short value);

            _position += 2;
            return Unsafe.As<short, ushort>(ref value);
        }


        public uint ReadFourByteInteger()
        {
            ValidateReceiveBuffer(4);

            var reader = new SequenceReader<byte>(_buffer);
            reader.Advance(_position);
            reader.TryReadBigEndian(out int value);

            _position += 4;
            return Unsafe.As<int, uint>(ref value);
        }


        public ReadOnlySequence<byte> ReadRemainingData()
        {
            var buffer = _buffer.Slice(_position);
            _position = _buffer.Length;
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
            var buffer = _buffer.Slice(_position, length);
            var result = Encoding.UTF8.GetString(buffer);

            _position += length;
            return result;
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

        public void SetBuffer(ReadOnlyMemory<byte> buffer)
        {
            SetBuffer(new ReadOnlySequence<byte>(buffer));
        }

        public void SetBuffer(ReadOnlySequence<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateReceiveBuffer(int length)
        {
            var bufferLength = _buffer.Length;
            var newPosition = _position + length;
            if (bufferLength < newPosition)
            {
                throw new MqttProtocolViolationException($"Expected at least {newPosition} bytes but there are only {bufferLength} bytes");
            }
        }
    }
}