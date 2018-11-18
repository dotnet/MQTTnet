using System;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.Serializer
{
    /// <summary>
    /// This is a custom implementation of a memory stream which provides only MQTTnet relevant features.
    /// The goal is to avoid lots of argument checks like in the original stream. The growth rule is the
    /// same as for the original MemoryStream in .net. Also this implementation allows accessing the internal
    /// buffer for all platforms and .net framework versions (which is not available at the regular MemoryStream).
    /// </summary>
    public class MqttPacketWriter
    {
        public static int MaxBufferSize = 4096;

        private byte[] _buffer = new byte[128];

        private int _position;

        public int Length { get; private set; }

        public static byte BuildFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (int)packetType << 4;
            fixedHeader |= flags;
            return (byte)fixedHeader;
        }

        public static ArraySegment<byte> EncodeVariableByteInteger(uint value)
        {
            if (value <= 0)
            {
                return new ArraySegment<byte>(new byte[1], 0, 1);
            }

            var buffer = new byte[4];
            var bufferOffset = 0;

            var x = value;
            do
            {
                var encodedByte = x % 128;
                x = x / 128;
                if (x > 0)
                {
                    encodedByte = encodedByte | 128;
                }

                buffer[bufferOffset] = (byte)encodedByte;
                bufferOffset++;
            } while (x > 0);

            return new ArraySegment<byte>(buffer, 0, bufferOffset);
        }

        public void WriteVariableLengthInteger(uint value)
        {
            Write(EncodeVariableByteInteger(value));
        }

        public void WriteWithLengthPrefix(string value)
        {
            WriteWithLengthPrefix(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        public void WriteWithLengthPrefix(byte[] value)
        {
            EnsureAdditionalCapacity(value.Length + 2);

            Write((ushort)value.Length);
            Write(value, 0, value.Length);
        }
        
        public void Write(byte @byte)
        {
            EnsureAdditionalCapacity(1);

            _buffer[_position] = @byte;
            IncreasePostition(1);
        }

        public void Write(ushort value)
        {
            EnsureAdditionalCapacity(2);

            _buffer[_position] = (byte)(value >> 8);
            IncreasePostition(1);
            _buffer[_position] = (byte)value;
            IncreasePostition(1);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            EnsureAdditionalCapacity(count);

            Array.Copy(buffer, offset, _buffer, _position, count);
            IncreasePostition(count);
        }

        public void Write(ArraySegment<byte> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        public void Write(MqttPacketWriter propertyWriter)
        {
            if (propertyWriter == null) throw new ArgumentNullException(nameof(propertyWriter));
            
            Write(propertyWriter._buffer, 0, propertyWriter.Length);
        }

        public void Reset()
        {
            Length = 5;
        }

        public void Seek(int position)
        {
            EnsureCapacity(position);
            _position = position;
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
        private void EnsureAdditionalCapacity(int additionalCapacity)
        {
            var freeSpace = _buffer.Length - _position;
            if (freeSpace >= additionalCapacity)
            {
                return;
            }

            EnsureCapacity(_buffer.Length + additionalCapacity - freeSpace);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int capacity)
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
        private void IncreasePostition(int length)
        {
            _position += length;

            if (_position > Length)
            {
                Length = _position;
            }
        }
    }
}
