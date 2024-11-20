// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace MQTTnet.Formatter
{
    /// <summary>
    ///     This is a custom implementation of a memory stream which provides only MQTTnet relevant features.
    ///     The goal is to avoid lots of argument checks like in the original stream. The growth rule is the
    ///     same as for the original MemoryStream in .net. Also this implementation allows accessing the internal
    ///     buffer for all platforms and .net framework versions (which is not available at the regular MemoryStream).
    /// </summary>
    public sealed class MqttBufferWriter
    {
        const uint VariableByteIntegerMaxValue = 268435455;
        const int EncodedStringMaxLength = 65535;

        readonly int _maxBufferSize;

        byte[] _buffer;
        int _position;

        public MqttBufferWriter(int bufferSize, int maxBufferSize)
        {
            _buffer = new byte[bufferSize];
            _maxBufferSize = maxBufferSize;
        }

        public int Length { get; private set; }

        public static byte BuildFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (int)packetType << 4;
            fixedHeader |= flags;
            return (byte)fixedHeader;
        }

        public void Cleanup()
        {
            // This method frees the used memory by shrinking the buffer. This is required because the buffer
            // is used across several messages. In general this is not a big issue because subsequent Ping packages
            // have the same size but a very big publish package with 100 MB of payload will increase the buffer
            // a lot and the size will never reduced. So this method tries to find a size which can be held in
            // memory for a long time without causing troubles.

            if (_buffer.Length <= _maxBufferSize)
            {
                return;
            }

            // Create a new and empty buffer. Do not use Array.Resize because it will copy all data from
            // the old array to the new one which is not required in this case.
            _buffer = new byte[_maxBufferSize];
        }

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        public static int GetVariableByteIntegerSize(uint value)
        {
            // From RFC: Table 2.4 Size of Remaining Length field

            // 0 (0x00) to 127 (0x7F)
            if (value <= 127)
            {
                return 1;
            }

            // 128 (0x80, 0x01) to 16 383 (0xFF, 0x7F)
            if (value <= 16383)
            {
                return 2;
            }

            // 16 384 (0x80, 0x80, 0x01) to 2 097 151 (0xFF, 0xFF, 0x7F)
            if (value <= 2097151)
            {
                return 3;
            }

            // 2 097 152 (0x80, 0x80, 0x80, 0x01) to 268 435 455 (0xFF, 0xFF, 0xFF, 0x7F)
            return 4;
        }

        public void Reset(int length)
        {
            _position = 0;
            Length = length;
        }

        public void Seek(int position)
        {
            EnsureCapacity(position);
            _position = position;
        }

        public void Write(MqttBufferWriter propertyWriter)
        {
            ArgumentNullException.ThrowIfNull(propertyWriter);

            Write(propertyWriter._buffer.AsSpan(0, propertyWriter.Length));
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            EnsureAdditionalCapacity(buffer.Length);

            buffer.CopyTo(_buffer.AsSpan(_position));

            IncreasePosition(buffer.Length);
        }

        public void WriteBinary(ReadOnlySpan<byte> value)
        {
            var valueLength = value.Length;
            EnsureAdditionalCapacity(valueLength + 2);

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), (ushort)valueLength);
            value.CopyTo(_buffer.AsSpan(_position + 2));

            IncreasePosition(valueLength + 2);
        }


        public void WriteByte(byte @byte)
        {
            EnsureAdditionalCapacity(1);

            _buffer[_position] = @byte;
            IncreasePosition(1);
        }

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                EnsureAdditionalCapacity(2);

                _buffer.AsSpan(_position, 2).Fill(default);

                IncreasePosition(2);
            }
            else
            {
                // Do not use Encoding.UTF8.GetByteCount(value);
                // UTF8 chars can have a max length of 4 and the used buffer increase *2 every time.
                // So the buffer should always have much more capacity left so that a correct value
                // here is only waste of CPU cycles.
                var byteCount = value.Length * 4;

                EnsureAdditionalCapacity(byteCount + 2);

                var writtenBytes = Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _position + 2);

                // From RFC: 1.5.4 UTF-8 Encoded String
                // Unless stated otherwise all UTF-8 encoded strings can have any length in the range 0 to 65,535 bytes.
                if (writtenBytes > EncodedStringMaxLength)
                {
                    throw new MqttProtocolViolationException($"The maximum string length is 65535. The current string has a length of {writtenBytes}.");
                }

                BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), (ushort)writtenBytes);

                IncreasePosition(writtenBytes + 2);
            }
        }

        public void WriteTwoByteInteger(ushort value)
        {
            EnsureAdditionalCapacity(2);

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), value);

            IncreasePosition(2);
        }

        public void WriteVariableByteInteger(uint value)
        {
            if (value <= 127)
            {
                _buffer[_position] = (byte)value;
                IncreasePosition(1);

                return;
            }

            if (value > VariableByteIntegerMaxValue)
            {
                throw new MqttProtocolViolationException($"The specified value ({value}) is too large for a variable byte integer.");
            }

            var size = 0;
            var x = value;
            do
            {
                var encodedByte = x % 128;
                x /= 128;
                if (x > 0)
                {
                    encodedByte |= 128;
                }

                _buffer[_position + size] = (byte)encodedByte;
                size++;
            } while (x > 0);

            IncreasePosition(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureAdditionalCapacity(int additionalCapacity)
        {
            var bufferLength = _buffer.Length;

            var freeSpace = bufferLength - _position;
            if (freeSpace >= additionalCapacity)
            {
                return;
            }

            EnsureCapacity(bufferLength + additionalCapacity - freeSpace);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureCapacity(int capacity)
        {
            var newBufferLength = _buffer.Length;

            if (newBufferLength >= capacity)
            {
                return;
            }

            while (newBufferLength < capacity)
            {
                newBufferLength *= 2;
            }

            // Array.Resize will create a new array and copy the existing data to the new one.
            Array.Resize(ref _buffer, newBufferLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IncreasePosition(int length)
        {
            _position += length;

            if (_position > Length)
            {
                // Also extend the position because we reached the end of the
                // pre allocated buffer.
                Length = _position;
            }
        }
    }
}