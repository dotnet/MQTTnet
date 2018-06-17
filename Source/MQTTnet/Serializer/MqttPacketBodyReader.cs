using System;
using System.Text;

namespace MQTTnet.Serializer
{
    public class MqttPacketBodyReader
    {
        private readonly byte[] _buffer;
        private int _offset;

        public MqttPacketBodyReader(byte[] buffer, int offset)
        {
            _buffer = buffer;
            _offset = offset;
        }

        public int Length => _buffer.Length - _offset;

        public bool EndOfStream => _offset == _buffer.Length;

        public byte ReadByte()
        {
            return _buffer[_offset++];
        }

        public ArraySegment<byte> ReadRemainingData()
        {
            return new ArraySegment<byte>(_buffer, _offset, _buffer.Length - _offset);
        }

        public ushort ReadUInt16()
        {
            var msb = _buffer[_offset++];
            var lsb = _buffer[_offset++];
            
            return (ushort)(msb << 8 | lsb);
        }

        public ArraySegment<byte> ReadWithLengthPrefix()
        {
            var length = ReadUInt16();

            var result = new ArraySegment<byte>(_buffer, _offset, length);
            _offset += length;

            return result;
        }

        public string ReadStringWithLengthPrefix()
        {
            var buffer = ReadWithLengthPrefix();
            return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
