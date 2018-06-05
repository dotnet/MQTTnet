using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Exceptions;
using MQTTnet.Internal;

namespace MQTTnet.Serializer
{
    public static class MqttPacketReader
    {
        public static async Task<MqttFixedHeader> ReadFixedHeaderAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            // The MQTT fixed header contains 1 byte of flags and at least 1 byte for the remaining data length.
            // So in all cases at least 2 bytes must be read for a complete MQTT packet.
            var buffer = new byte[2];
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await channel.ReadAsync(buffer, 0, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                {
                    ExceptionHelper.ThrowGracefulSocketClose();
                }

                totalBytesRead += bytesRead;
            }

            var hasRemainingLength = buffer[1] != 0;
            if (!hasRemainingLength)
            {
                return new MqttFixedHeader(buffer[0], 0);
            }

            var bodyLength = await ReadBodyLengthAsync(channel, buffer[1], cancellationToken);
            return new MqttFixedHeader(buffer[0], bodyLength);
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            var buffer = stream.ReadBytes(2);

            var temp = buffer[0];
            buffer[0] = buffer[1];
            buffer[1] = temp;

            return BitConverter.ToUInt16(buffer, 0);
        }

        public static string ReadStringWithLengthPrefix(this Stream stream)
        {
            var buffer = stream.ReadWithLengthPrefix();
            if (buffer.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public static byte[] ReadWithLengthPrefix(this Stream stream)
        {
            var length = stream.ReadUInt16();
            if (length == 0)
            {
                return new byte[0];
            }

            return stream.ReadBytes(length);
        }

        public static byte[] ReadRemainingData(this Stream stream)
        {
            return stream.ReadBytes((int)(stream.Length - stream.Position));
        }

        private static byte[] ReadBytes(this Stream stream, int count)
        {
            var buffer = new byte[count];
            var readBytes = stream.Read(buffer, 0, count);

            if (readBytes != count)
            {
                throw new InvalidOperationException($"Unable to read {count} bytes from the stream.");
            }

            return buffer;
        }

        private static async Task<int> ReadBodyLengthAsync(IMqttChannel channel, byte initialEncodedByte, CancellationToken cancellationToken)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = (byte)(initialEncodedByte & 127) * multiplier;
            int encodedByte = initialEncodedByte;
            var buffer = new byte[1];

            while ((encodedByte & 128) != 0)
            {
                var readCount = await channel.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
                if (readCount <= 0)
                {
                    ExceptionHelper.ThrowGracefulSocketClose();
                }

                encodedByte = buffer[0];

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }

                multiplier *= 128;
            }

            return value;
        }
    }
}
