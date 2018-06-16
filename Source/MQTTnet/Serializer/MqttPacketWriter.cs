using System;
using System.IO;
using System.Runtime.InteropServices;
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

        public static void WriteUInt16(this MemoryBufferWriter stream, ushort value)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(stream.GetSpan(), value);
            stream.Advance(2);
        }

        public static void WriteWithLengthPrefix(this MemoryBufferWriter stream, string value)
        {
            //TODO enable this once System.Text.Primitives is released
            //var unicodeBytes = MemoryMarshal.Cast<char, byte>((value ?? string.Empty).AsSpan());
            //System.Buffers.Text.Encodings.Utf8.FromUtf16(unicodeBytes, stream.GetSpan().Slice(2), out _, out var written);
            //stream.Write((ushort)written);
            //stream.Advance(written);
            stream.WriteWithLengthPrefix(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        public static void WriteWithLengthPrefix(this MemoryBufferWriter stream, byte[] value)
        {
            var length = (ushort)value.Length;

            stream.WriteUInt16(length);
            stream.Write(value, 0, length);
        }

        public static int GetHeaderLength(int bodyLength)
        {
            if (bodyLength < 128)
            {
                return 2;
            }

            if (bodyLength < 128 * 128)
            {
                return 3;
            }

            if (bodyLength < 128 * 128 * 128)
            {
                return 4;
            }
            return 5;
        }

        public static void WriteBodyLength(int length, byte[] buffer)
        {
            var bufferOffset = 1;
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
        }
    }
}
