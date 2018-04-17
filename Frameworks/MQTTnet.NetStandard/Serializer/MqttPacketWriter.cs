using System;
using System.IO;
using System.Linq;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.Serializer
{
    public sealed class MqttPacketWriter : BinaryWriter
    {
        public MqttPacketWriter(Stream stream)
            : base(stream, Encoding.UTF8, true)
        {
        }

        public static byte BuildFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (int)packetType << 4;
            fixedHeader |= flags;
            return (byte)fixedHeader;
        }

        public override void Write(ushort value)
        {
            var buffer = BitConverter.GetBytes(value);
            Write(buffer[1], buffer[0]);
        }

        public new void Write(params byte[] values)
        {
            base.Write(values);
        }

        public new void Write(byte value)
        {
            base.Write(value);
        }

        public void Write(ByteWriter value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Write(value.Value);
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

        public static byte[] EncodeRemainingLength(int length)
        {
            if (length <= 0)
            {
                return new[] { (byte)0 };
            }

            var bytes = new byte[4];
            var offset = 0;

            // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var x = length;
            do
            {
                var encodedByte = x % 128;
                x = x / 128;
                if (x > 0)
                {
                    encodedByte = encodedByte | 128;
                }

                bytes[offset] = (byte)encodedByte;

                offset++;
            } while (x > 0);

            Array.Resize(ref bytes, offset);
            return bytes;
        }
    }
}
