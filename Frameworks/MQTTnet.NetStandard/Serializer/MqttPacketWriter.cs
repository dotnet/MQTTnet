using System;
using System.IO;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.Serializer
{
    public static class MqttPacketWriter
    {
        public static byte BuildFixedHeader(MqttControlPacketType packetType, byte flags = 0)
        {
            var fixedHeader = (int)packetType << 4;
            fixedHeader |= flags;
            return (byte)fixedHeader;
        }

        public static void Write(this Stream stream, ushort value)
        {
            var buffer = BitConverter.GetBytes(value);
            stream.WriteByte(buffer[1]);
            stream.WriteByte(buffer[0]);
        }

        public static void Write(this Stream stream, ByteWriter value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            stream.WriteByte(value.Value);
        }

        public static void WriteWithLengthPrefix(this Stream stream, string value)
        {
            stream.WriteWithLengthPrefix(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        public static void WriteWithLengthPrefix(this Stream stream, byte[] value)
        {
            var length = (ushort)value.Length;

            stream.Write(length);
            stream.Write(value, 0, length);
        }

        public static ArraySegment<byte> EncodeRemainingLength(int length)
        {
            // write the encoded remaining length right aligned on the 4 byte buffer
            if (length <= 0)
            {
                return new ArraySegment<byte>(new byte[1], 0, 1);
            }

            var buffer = new byte[4];
            var bufferOffset = 0;

            // Algorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var x = length;
            do
            {
                var encodedByte = x % 128;
                x = x / 128;
                if (x > 0)
                {
                    encodedByte = encodedByte | 128;
                }

                buffer[bufferOffset] = (byte)encodedByte;
                bufferOffset++;
            } while (x > 0);

            return new ArraySegment<byte>(buffer, 0, bufferOffset);
        }
    }
}
