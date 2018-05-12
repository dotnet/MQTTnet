using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Serializer
{
    public static class MqttPacketReader
    {
        public static async Task<MqttPacketHeader> ReadHeaderAsync(IMqttChannel stream, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            // Wait for the next package which starts with the header. At this point there will probably
            // some large delay and thus the thread should be put back to the pool (await). So ReadByte()
            // is not an option here.
            var buffer = new byte[1];
            var readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (readCount <= 0)
            {
                return null;
            }

            var fixedHeader = buffer[0];
            var controlPacketType = (MqttControlPacketType)(fixedHeader >> 4);
            var bodyLength = await ReadBodyLengthAsync(stream, cancellationToken).ConfigureAwait(false);

            return new MqttPacketHeader
            {
                FixedHeader = fixedHeader,
                ControlPacketType = controlPacketType,
                BodyLength = bodyLength
            };
        }

        public static byte ReadByte(this ref ReadOnlySpan<byte> stream)
        {
            var result = stream[0];
            stream = stream.Slice(1);
            return result;
        }

        public static ushort ReadUInt16(this ref ReadOnlySpan<byte> stream)
        {
            var result = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(stream);
            stream = stream.Slice(2);
            return result;
        }

        public static string ReadStringWithLengthPrefix(this ref ReadOnlySpan<byte> stream)
        {
            var buffer = stream.ReadWithLengthPrefix();
            if (buffer.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public static byte[] ReadWithLengthPrefix(this ref ReadOnlySpan<byte> stream)
        {
            var length = stream.ReadUInt16();
            if (length == 0)
            {
                return new byte[0];
            }

            var result = stream.Slice(0, length).ToArray();
            stream = stream.Slice(length);
            return result;
        }

        private static async Task<int> ReadBodyLengthAsync(IMqttChannel stream, CancellationToken cancellationToken)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            int encodedByte;
            var buffer = new byte[1];

            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                var readCount = await stream.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
                if (readCount <= 0)
                {
                    throw new MqttCommunicationException("Connection closed while reading remaining length data.");
                }

                encodedByte = buffer[0];

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return value;
        }
    }
}
