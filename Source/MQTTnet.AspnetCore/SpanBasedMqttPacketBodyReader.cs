using MQTTnet.Formatter;
using System;
using System.Buffers.Binary;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class SpanBasedMqttPacketBodyReader : IMqttPacketBodyReader
    {
        private ReadOnlyMemory<byte> _buffer;
        private int _offset;
        
        public void SetBuffer(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public ulong Length => (ulong)_buffer.Length;

        public bool EndOfStream => _buffer.Length.Equals(_offset);
        
        public ulong Offset => (ulong)_offset;

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);
            return _buffer.Span[_offset++];
        }

        public byte[] ReadRemainingData()
        {
            return _buffer.ToArray();
        }

        public ushort ReadUInt16()
        {
            ValidateReceiveBuffer(2);

            var result = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span);
            _offset += 2;
            return result;
        }

        public byte[] ReadWithLengthPrefix()
        {
            return ReadSegmentWithLengthPrefix().ToArray();
        }

        private ReadOnlyMemory<byte> ReadSegmentWithLengthPrefix()
        {
            var length = ReadUInt16();

            ValidateReceiveBuffer(length);

            var result = _buffer.Slice(_offset, length);
            _offset += length;
            return result;
        }

        private void ValidateReceiveBuffer(ushort length)
        {
            if (_buffer.Length < _offset + length)
            {
                throw new ArgumentOutOfRangeException(nameof(_buffer), $"expected at least {_offset + length} bytes but there are only {_buffer.Length} bytes");
            }
        }

        public unsafe string ReadStringWithLengthPrefix()
        {
            var buffer = ReadSegmentWithLengthPrefix();
            fixed (byte* bytes = &buffer.Span.GetPinnableReference())
            {
                var result = Encoding.UTF8.GetString(bytes, buffer.Length);
                return result;
            }
        }

        public ushort ReadTwoByteInteger()
        {
            throw new NotImplementedException();
        }

        public uint ReadFourByteInteger()
        {
            throw new NotImplementedException();
        }

        public uint ReadVariableLengthInteger()
        {
            throw new NotImplementedException();
        }

        public bool ReadBoolean()
        {
            throw new NotImplementedException();
        }

        public void Seek(ulong position)
        {
            throw new NotImplementedException();
        }
    }
}
