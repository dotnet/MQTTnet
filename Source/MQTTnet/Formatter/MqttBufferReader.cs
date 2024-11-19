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
        ReadOnlySequence<byte> _buffer = ReadOnlySequence<byte>.Empty;
        long _position;

        public long BytesLeft => _buffer.Length - _position;

        public bool EndOfStream => BytesLeft == 0;

        public long Position => _position;

        public ReadOnlySequence<byte> ReadBinaryData()
        {
            var length = ReadTwoByteInteger();

            if (length == 0)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            ValidateReceiveBuffer(length);

            var result = _buffer.Slice(_position, length);
            _position += length;

            return result;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);

            var reader = new SequenceReader<byte>(_buffer);
            reader.Advance(_position);
            reader.TryRead(out var value);
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
            var bufferLength = BytesLeft;
            if (bufferLength == 0)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            var buffer = _buffer.Slice(_position, bufferLength);
            _position += bufferLength;

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

            // AsSpan() version is slightly faster. Not much but at least a little bit.
            var result = Encoding.UTF8.GetString(_buffer.Slice(_position, length));

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
            var newPosition = _position + length;
            var maxPosition = _buffer.Length;
            if (maxPosition < newPosition)
            {
                throw new MqttProtocolViolationException($"Expected at least {newPosition} bytes but there are only {maxPosition} bytes");
            }
        }
    }
}