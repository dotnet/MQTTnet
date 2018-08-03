using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MQTTnet.Exceptions;

namespace MQTTnet.Formatter
{
    public class MqttPacketBodyReader : IMqttPacketBodyReader
    {
        private readonly byte[] _buffer;
        private readonly ulong _initialOffset;
        private readonly ulong _length;

        public MqttPacketBodyReader(byte[] buffer, int offset, int length)
         : this(buffer, (ulong)offset, (ulong)length)
        {
        }

        public MqttPacketBodyReader(byte[] buffer, ulong offset, ulong length)
        {
            _buffer = buffer;
            _initialOffset = offset;
            Offset = offset;
            _length = length;
        }

        public ulong Offset { get; private set; }

        public ulong Length => _length - Offset;

        public bool EndOfStream => Offset == _length;

        public void Seek(ulong position)
        {
            Offset = _initialOffset + position;
        }

        public ArraySegment<byte> Read(uint length)
        {
            ValidateReceiveBuffer(length);

            var buffer = new ArraySegment<byte>(_buffer, (int)Offset, (int)length);
            Offset += length;
            return buffer;
        }

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);
            return _buffer[Offset++];
        }

        public bool ReadBoolean()
        {
            ValidateReceiveBuffer(1);
            var buffer = _buffer[Offset++];

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
            return new ArraySegment<byte>(_buffer, (int)Offset, (int)(_length - Offset)).ToArray();
        }

        public ushort ReadTwoByteInteger()
        {
            ValidateReceiveBuffer(2);

            var msb = _buffer[Offset++];
            var lsb = _buffer[Offset++];
            
            return (ushort)(msb << 8 | lsb);
        }

        public uint ReadFourByteInteger()
        {
            ValidateReceiveBuffer(4);

            var byte0 = _buffer[Offset++];
            var byte1 = _buffer[Offset++];
            var byte2 = _buffer[Offset++];
            var byte3 = _buffer[Offset++];

            return (uint)(byte0 << 24 | byte1 << 16 | byte2 << 8 | byte3);
        }

        public byte[] ReadWithLengthPrefix()
        {
            return ReadSegmentWithLengthPrefix().ToArray();
        }

        private ArraySegment<byte> ReadSegmentWithLengthPrefix()
        {
            var length = ReadTwoByteInteger();

            ValidateReceiveBuffer(length);

            var result = new ArraySegment<byte>(_buffer, (int)Offset, length);
            Offset += length;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateReceiveBuffer(uint length)
        {
            if (_length < Offset + length)
            {
                throw new ArgumentOutOfRangeException(nameof(_buffer), $"Expected at least {Offset + length} bytes but there are only {_length} bytes");
            }
        }

        public string ReadStringWithLengthPrefix()
        {
            var buffer = ReadSegmentWithLengthPrefix();
            return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
