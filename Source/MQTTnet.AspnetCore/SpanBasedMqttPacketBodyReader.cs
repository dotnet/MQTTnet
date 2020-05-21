using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using System;
using System.Buffers.Binary;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class SpanBasedMqttPacketBodyReader : IMqttPacketBodyReader
    {
        ReadOnlyMemory<byte> _buffer;

        int _offset;
        
        public void SetBuffer(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public int Length => _buffer.Length;

        public bool EndOfStream => _buffer.Length.Equals(_offset);
        
        public int Offset => _offset;

        public byte ReadByte()
        {
            return _buffer.Span[_offset++];
        }

        public byte[] ReadRemainingData()
        {
            return _buffer.Slice(_offset).ToArray();
        }

        public byte[] ReadWithLengthPrefix()
        {
            return ReadSegmentWithLengthPrefix().ToArray();
        }
        
        public unsafe string ReadStringWithLengthPrefix()
        {
            var buffer = ReadSegmentWithLengthPrefix();
            if (buffer.Length == 0)
            {
                return string.Empty;
            }

            fixed (byte* bytes = &buffer.GetPinnableReference())
            {
                var result = Encoding.UTF8.GetString(bytes, buffer.Length);
                return result;
            }
        }

        public ushort ReadTwoByteInteger()
        {
            var result = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(_offset));
            _offset += 2;
            return result;
        }

        public uint ReadFourByteInteger()
        {
            var result = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Span.Slice(_offset));
            _offset += 4;
            return result;
        }

        public uint ReadVariableLengthInteger()
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

        public bool ReadBoolean()
        {
            var buffer = ReadByte();

            if (buffer == 0)
            {
                return false;
            }

            if (buffer == 1)
            {
                return true;
            }

            throw new MqttProtocolViolationException("Boolean values can be 0 or 1 only.");
        }

        public void Seek(int position)
        {
            _offset = position;
        }

        ReadOnlySpan<byte> ReadSegmentWithLengthPrefix()
        {
            var span = _buffer.Span;
            var length = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(_offset));

            var result = span.Slice(_offset + 2, length);
            _offset += 2 + length;
            return result;
        }
    }
}
