using System;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    /// <summary>
    /// This is a custom implementation of a memory stream which provides only MQTTnet relevant features.
    /// The goal is to avoid lots of argument checks like in the original stream. The growth rule is the
    /// same as for the original MemoryStream in .net. Also this implementation allows accessing the internal
    /// buffer for all platforms and .net framework versions (which is not available at the regular MemoryStream).
    /// </summary>
    public sealed class MqttPacketWriter : IMqttPacketWriter
    {
        public static int InitialBufferSize = 4096;
        public static int MaxBufferSize = 4096 + 4;

        byte[] _buffer = new byte[InitialBufferSize];

        int _offset;

        public int Length { get; private set; }

        public static byte BuildFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (int)packetType << 4;
            fixedHeader |= flags;
            return (byte)fixedHeader;
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

        public void WriteVariableLengthInteger(uint value)
        {
            if (value == 0)
            {
                _buffer[_offset] = 0;
                IncreasePosition(1);

                return;
            }

            if (value <= 127)
            {
                _buffer[_offset] = (byte)value;
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

                _buffer[_offset + size] = (byte)encodedByte;
                size++;
            } while (x > 0);

            IncreasePosition(size);
        }

        public void WriteWithLengthPrefix(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                EnsureAdditionalCapacity(2);

                _buffer[_offset] = 0;
                _buffer[_offset + 1] = 0;

                IncreasePosition(2);
            }
            else
            {
                var bufferSize = Encoding.UTF8.GetByteCount(value);

                EnsureAdditionalCapacity(bufferSize + 2);

                _buffer[_offset] = (byte)(bufferSize >> 8);
                _buffer[_offset + 1] = (byte)bufferSize;

                Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _offset + 2);

                IncreasePosition(bufferSize + 2);
            }
        }

        public void WriteWithLengthPrefix(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                EnsureAdditionalCapacity(2);

                _buffer[_offset] = 0;
                _buffer[_offset + 1] = 0;

                IncreasePosition(2);
            }
            else
            {
                EnsureAdditionalCapacity(value.Length + 2);

                _buffer[_offset] = (byte)(value.Length >> 8);
                _buffer[_offset + 1] = (byte)value.Length;

                Array.Copy(value, 0, _buffer, _offset + 2, value.Length);
                IncreasePosition(value.Length + 2);
            }
        }

        public void Write(byte @byte)
        {
            EnsureAdditionalCapacity(1);

            _buffer[_offset] = @byte;
            IncreasePosition(1);
        }

        public void Write(ushort value)
        {
            EnsureAdditionalCapacity(2);

            _buffer[_offset] = (byte)(value >> 8);
            IncreasePosition(1);
            _buffer[_offset] = (byte)value;
            IncreasePosition(1);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            if (count == 0)
            {
                return;
            }

            EnsureAdditionalCapacity(count);

            Array.Copy(buffer, offset, _buffer, _offset, count);
            IncreasePosition(count);
        }

        public void Write(IMqttPacketWriter propertyWriter)
        {
            if (propertyWriter is MqttPacketWriter writer)
            {
                if (writer.Length == 0)
                {
                    return;
                }

                Write(writer._buffer, 0, writer.Length);
                return;
            }

            if (propertyWriter == null)
            {
                throw new ArgumentNullException(nameof(propertyWriter));
            }

            throw new InvalidOperationException($"{nameof(propertyWriter)} must be of type {nameof(MqttPacketWriter)}");
        }

        public void Reset(int length)
        {
            Length = length;
        }

        public void Seek(int position)
        {
            EnsureCapacity(position);
            _offset = position;
        }

        public byte[] GetBuffer()
        {
            return _buffer;
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

            Array.Resize(ref _buffer, MaxBufferSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureAdditionalCapacity(int additionalCapacity)
        {
            var freeSpace = _buffer.Length - _offset;
            if (freeSpace >= additionalCapacity)
            {
                return;
            }

            EnsureCapacity(_buffer.Length + additionalCapacity - freeSpace);
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

            Array.Resize(ref _buffer, newBufferLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IncreasePosition(int length)
        {
            _offset += length;

            if (_offset > Length)
            {
                Length = _offset;
            }
        }
    }
}
