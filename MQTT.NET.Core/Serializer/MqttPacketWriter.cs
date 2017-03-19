using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Serializer
{
    public sealed class MqttPacketWriter : IDisposable
    {
        private readonly MemoryStream _buffer = new MemoryStream();

        public void InjectFixedHeader(byte fixedHeader)
        {
            if (_buffer.Length == 0)
            {
                Write(fixedHeader);
                Write(0);
                return;
            }

            var remainingLength = (int)_buffer.Length;
            using (var buffer = new MemoryStream())
            {
                _buffer.WriteTo(buffer);
                _buffer.SetLength(0);

                _buffer.WriteByte(fixedHeader);

                // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
                var x = remainingLength;
                do
                {
                    var encodedByte = (byte)(x % 128);
                    x = x / 128;
                    if (x > 0)
                    {
                        encodedByte = (byte)(encodedByte | 128);
                    }

                    _buffer.WriteByte(encodedByte);
                } while (x > 0);

                buffer.Position = 0;
                buffer.WriteTo(_buffer);
            }
        }

        public void InjectFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (byte)((byte)packetType << 4);
            fixedHeader |= flags;
            InjectFixedHeader(fixedHeader);
        }

        public void Write(byte value)
        {
            _buffer.WriteByte(value);
        }

        public void Write(char value)
        {
            _buffer.WriteByte((byte)value);
        }

        public void Write(ushort value)
        {
            var buffer = BitConverter.GetBytes(value);
            _buffer.WriteByte(buffer[1]);
            _buffer.WriteByte(buffer[0]);
        }

        public void Write(ByteWriter value)
        {
            _buffer.WriteByte(value.Value);
        }

        public void Write(byte[] value)
        {
            _buffer.Write(value, 0, value.Length);
        }

        public void WriteWithLengthPrefix(string value)
        {
            WriteWithLengthPrefix(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        public void WriteWithLengthPrefix(byte[] value)
        {
            var length = (ushort)value.Length;

            Write(length);
            Write(value);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }

        public async Task WriteToAsync(IMqttTransportChannel destination)
        {
            await destination.WriteAsync(_buffer.ToArray());
        }
    }
}
