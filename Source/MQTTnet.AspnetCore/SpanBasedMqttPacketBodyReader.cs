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
            return _buffer.Span[_offset++];
        }

        public byte[] ReadRemainingData()
        {
            return _buffer.Slice(_offset).ToArray();
        }

        public ushort ReadUInt16()
        {
            var result = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(_offset));
            _offset += 2;
            return result;
        }

        public byte[] ReadWithLengthPrefix()
        {
            return ReadSegmentWithLengthPrefix().ToArray();
        }

        private ReadOnlySpan<byte> ReadSegmentWithLengthPrefix()
        {
            var span = _buffer.Span;
            var length = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(_offset));

            var result = span.Slice(_offset+2, length);
            _offset += 2 + length;
            return result;
        }
        
        public unsafe string ReadStringWithLengthPrefix()
        {
            var buffer = ReadSegmentWithLengthPrefix();
            fixed (byte* bytes = &buffer.GetPinnableReference())
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
