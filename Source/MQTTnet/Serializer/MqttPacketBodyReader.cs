using System;
using System.Text;

namespace MQTTnet.Serializer
{
    public class MqttPacketBodyReader
    {
        private readonly byte[] _buffer;
        private readonly int _length;

        private int _offset;
        
        public MqttPacketBodyReader(byte[] buffer, int offset, int length)
        {
            _buffer = buffer;
            _offset = offset;
            _length = length;
        }

        public int Length => _length - _offset;

        public bool EndOfStream => _offset == _length;

        public byte ReadByte()
        {
            ValidateReceiveBuffer(1);
            return _buffer[_offset++];
        }

        public ArraySegment<byte> ReadRemainingData()
        {
            return new ArraySegment<byte>(_buffer, _offset, _length - _offset);
        }

        public ushort ReadUInt16()
        {
            ValidateReceiveBuffer(2);

            var msb = _buffer[_offset++];
            var lsb = _buffer[_offset++];
            
            return (ushort)(msb << 8 | lsb);
        }

        public ArraySegment<byte> ReadWithLengthPrefix()
        {
            var length = ReadUInt16();

            ValidateReceiveBuffer(length);

            var result = new ArraySegment<byte>(_buffer, _offset, length);
            _offset += length;

            return result;
        }

        private void ValidateReceiveBuffer(ushort length)
        {
            if (_length < _offset + length)
            {
                throw new ArgumentOutOfRangeException(nameof(_buffer), $"expected at least {_offset + length} bytes but there are only {_length} bytes");
            }
        }

        public string ReadStringWithLengthPrefix()
        {
            var buffer = ReadWithLengthPrefix();
            return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
