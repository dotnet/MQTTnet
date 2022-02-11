// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    /// <summary>
    ///     This is a custom implementation of a memory stream which provides only MQTTnet relevant features.
    ///     The goal is to avoid lots of argument checks like in the original stream. The growth rule is the
    ///     same as for the original MemoryStream in .net. Also this implementation allows accessing the internal
    ///     buffer for all platforms and .net framework versions (which is not available at the regular MemoryStream).
    /// </summary>
    public sealed class MqttPacketWriter : IMqttPacketWriter
    {
        public static int InitialBufferSize { get; set; } = 4096;
        public static int MaxBufferSize { get; set; } = 4096 * 4;

        byte[] _buffer = new byte[InitialBufferSize];
        int _position;
        int _length;

        public int Length => _length;

        public static byte BuildFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (int)packetType << 4;
            fixedHeader |= flags;
            return (byte)fixedHeader;
        }

        public void FreeBuffer()
        {
            // This method frees the used memory by shrinking the buffer. This is required because the buffer
            // is used across several messages. In general this is not a big issue because subsequent Ping packages
            // have the same size but a very big publish package with 100 MB of payload will increase the buffer
            // a lot and the size will never reduced. So this method tries to find a size which can be held in
            // memory for a long time without causing troubles.

            if (_buffer.Length < MaxBufferSize)
            {
                return;
            }

            // Create a new and empty buffer. Do not use Array.Resize because it will copy all data from
            // the old array to the new one which is not required in this case.
            _buffer = new byte[MaxBufferSize];
        }

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        public static int GetLengthOfVariableInteger(uint value)
        {
            var result = 0;
            var x = value;
            do
            {
                x /= 128;
                result++;
            } while (x > 0);

            return result;
        }

        public void Reset(int length)
        {
            _position = 0;
            _length = length;
        }

        public void Seek(int position)
        {
            EnsureCapacity(position);
            _position = position;
        }

        public void Write(byte @byte)
        {
            EnsureAdditionalCapacity(1);

            _buffer[_position] = @byte;
            IncreasePosition(1);
        }

        public void Write(ushort value)
        {
            EnsureAdditionalCapacity(2);

            _buffer[_position] = (byte)(value >> 8);
            IncreasePosition(1);
            _buffer[_position] = (byte)value;
            IncreasePosition(1);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count == 0)
            {
                return;
            }

            EnsureAdditionalCapacity(count);

            Array.Copy(buffer, offset, _buffer, _position, count);
            IncreasePosition(count);
        }

        public void Write(IMqttPacketWriter propertyWriter)
        {
            if (propertyWriter is MqttPacketWriter writer)
            {
                Write(writer._buffer, 0, writer.Length);
                return;
            }

            if (propertyWriter == null)
            {
                throw new ArgumentNullException(nameof(propertyWriter));
            }

            throw new InvalidOperationException($"{nameof(propertyWriter)} must be of type {nameof(MqttPacketWriter)}");
        }

        public void WriteVariableLengthInteger(uint value)
        {
            if (value == 0)
            {
                _buffer[_position] = 0;
                IncreasePosition(1);

                return;
            }

            if (value <= 127)
            {
                _buffer[_position] = (byte)value;
                IncreasePosition(1);

                return;
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

        public void WriteWithLengthPrefix(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                EnsureAdditionalCapacity(2);

                _buffer[_position] = 0;
                _buffer[_position + 1] = 0;

                IncreasePosition(2);
            }
            else
            {
                var bufferSize = Encoding.UTF8.GetByteCount(value);

                EnsureAdditionalCapacity(bufferSize + 2);

                _buffer[_position] = (byte)(bufferSize >> 8);
                _buffer[_position + 1] = (byte)bufferSize;

                Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _position + 2);

                IncreasePosition(bufferSize + 2);
            }
        }

        public void WriteWithLengthPrefix(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                EnsureAdditionalCapacity(2);

                _buffer[_position] = 0;
                _buffer[_position + 1] = 0;

                IncreasePosition(2);
            }
            else
            {
                var valueLength = value.Length;
                
                EnsureAdditionalCapacity(valueLength + 2);

                _buffer[_position] = (byte)(valueLength >> 8);
                _buffer[_position + 1] = (byte)valueLength;

                Array.Copy(value, 0, _buffer, _position + 2, valueLength);
                IncreasePosition(valueLength + 2);
            }
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

            // Array.Resize will create a new array and copy the existing
            // data to the new one.
            Array.Resize(ref _buffer, newBufferLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IncreasePosition(int length)
        {
            _position += length;

            if (_position > _length)
            {
                _length = _position;
            }
        }
    }
}