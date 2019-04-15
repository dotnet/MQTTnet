using MQTTnet.Formatter;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class SpanBasedMqttPacketWriter : IMqttPacketWriter
    {
        private readonly ArrayPool<byte> _pool;

        public SpanBasedMqttPacketWriter()
        {
            _pool = ArrayPool<byte>.Create();
            
        }

        private byte[] _buffer;
        private int _position;

        public int Length { get; set; }

        public void FreeBuffer()
        {
            _pool.Return(_buffer);
        }

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        public void Reset(int v)
        {
            _buffer = _pool.Rent(1500);
            Length = v;
            _position = v;
        }

        public void Seek(int v)
        {
            _position = v;
        }

        public void Write(byte value)
        {
            GrowIfNeeded(1);
            _buffer[_position] = value;
            Commit(1);
        }

        public void Write(ushort value)
        {
            GrowIfNeeded(2);

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), value);
            Commit(2);
        }

        public void Write(IMqttPacketWriter propertyWriter)
        {
            if (propertyWriter is SpanBasedMqttPacketWriter writer)
            {
                GrowIfNeeded(1);
            }

            throw new InvalidOperationException($"{nameof(propertyWriter)} must be of type {typeof(SpanBasedMqttPacketWriter).Name}");
        }

        public void Write(byte[] payload, int start, int length)
        {
            GrowIfNeeded(length);

            payload.AsSpan(start, length).CopyTo(_buffer.AsSpan(_position));
            Commit(length);
        }

        public void WriteVariableLengthInteger(uint value)
        {
            GrowIfNeeded(4);
            
            var x = value;
            do
            {
                var encodedByte = x % 128;
                x = x / 128;
                if (x > 0)
                {
                    encodedByte = encodedByte | 128;
                }

                _buffer[_position] = (byte)encodedByte;
                Commit(1);
            } while (x > 0);
        }

        public void WriteWithLengthPrefix(string value)
        {
            var bytesLength = Encoding.UTF8.GetByteCount(value ?? string.Empty);
            GrowIfNeeded(bytesLength + 2);

            Write((ushort)bytesLength);
            Encoding.UTF8.GetBytes(value ?? string.Empty, 0, value?.Length ?? 0, _buffer, _position);
            Commit(bytesLength);
        }

        public void WriteWithLengthPrefix(byte[] payload)
        {
            GrowIfNeeded(payload.Length + 2);
            
            Write((ushort)payload.Length);
            payload.CopyTo(_buffer, _position);
            Commit(payload.Length);
        }

        private void Commit(int count)
        {
            if (_position == Length)
            {
                Length += count;
            }

            _position += count;
        }

        private void GrowIfNeeded(int requiredAdditional) 
        {
            var requiredTotal = _position + requiredAdditional;
            if (_buffer.Length >= requiredTotal)
            {
                return;
            }

            var newBufferLength = _buffer.Length;
            while (newBufferLength < requiredTotal)
            {
                newBufferLength *= 2;
            }

            var newBuffer = _pool.Rent(newBufferLength);
            Array.Copy(_buffer, newBuffer, _buffer.Length);
            _pool.Return(_buffer);
            _buffer = newBuffer;
        }
    }
}
