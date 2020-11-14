using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Exceptions;

namespace MQTTnet.Formatter
{
    public sealed class MqttPacketBodyReader : IMqttPacketBodyReader
    {
        readonly byte[] _buffer;
        readonly int _initialOffset;
        readonly int _length;

        int _offset;

        public MqttPacketBodyReader(byte[] buffer, int offset, int length)
        {
            _buffer = buffer;
            _initialOffset = offset;
            _offset = offset;
            _length = length;
        }

        public int Offset => _offset;

        public int Length => _length - _offset;

        public bool EndOfStream => _offset == _length;

        public void Seek(int position)
        {
            _offset = _initialOffset + position;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);

            return _buffer[_offset++];
        }
        
        public bool ReadBoolean()
        {
            ValidateReceiveBuffer(1);

            var buffer = _buffer[_offset++];

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

        public byte[] ReadRemainingData()
        {
            var bufferLength = _length - _offset;
            var buffer = new byte[bufferLength];
            Array.Copy(_buffer, _offset, buffer, 0, bufferLength);

            return buffer;
        }

        public ushort ReadTwoByteInteger()
        {
            ValidateReceiveBuffer(2);

            var msb = _buffer[_offset++];
            var lsb = _buffer[_offset++];
            
            return (ushort)(msb << 8 | lsb);
        }

        public uint ReadFourByteInteger()
        {
            ValidateReceiveBuffer(4);

            var byte0 = _buffer[_offset++];
            var byte1 = _buffer[_offset++];
            var byte2 = _buffer[_offset++];
            var byte3 = _buffer[_offset++];

            return (uint)(byte0 << 24 | byte1 << 16 | byte2 << 8 | byte3);
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
        
        public byte[] ReadWithLengthPrefix()
        {
            return ReadSegmentWithLengthPrefix().ToArray();
        }

        private ArraySegment<byte> ReadSegmentWithLengthPrefix()
        {
            var length = ReadTwoByteInteger();

            ValidateReceiveBuffer(length);

            var result = new ArraySegment<byte>(_buffer, _offset, length);
            _offset += length;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateReceiveBuffer(int length)
        {
            if (_length < _offset + length)
            {
                throw new MqttProtocolViolationException($"Expected at least {_offset + length} bytes but there are only {_length} bytes");
            }
        }

        public string ReadStringWithLengthPrefix()
        {
            var buffer = ReadSegmentWithLengthPrefix();
            return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
