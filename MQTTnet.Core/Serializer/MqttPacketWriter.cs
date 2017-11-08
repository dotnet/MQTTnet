using System;
using System.IO;
using System.Text;
using MQTTnet.Core.Protocol;
using System.Collections.Generic;

namespace MQTTnet.Core.Serializer
{
    public sealed class MqttPacketWriter : BinaryWriter
    {
        public MqttPacketWriter(Stream stream)
            : base(stream)
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

        public static void WriteRemainingLength(int length, List<byte> target)
        {
            if (length == 0)
            {
                target.Add(0);
                return;
            }

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

                target.Add((byte)encodedByte);
            } while (x > 0);
        }
    }
}
