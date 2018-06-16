using System;
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
            // async/await is used here because the next packet is received in a couple of minutes so the performance
            // impact is acceptable according to a useless waiting thread.
            var buffer = new byte[2];
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await channel.ReadAsync(buffer, 0, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    ExceptionHelper.ThrowGracefulSocketClose();
                }

                totalBytesRead += bytesRead;
            }

            var hasRemainingLength = buffer[1] != 0;
            if (!hasRemainingLength)
            {
                return new MqttFixedHeader(buffer[0], 0);
            }

            var bodyLength = ReadBodyLength(channel, buffer[1], cancellationToken);
            return new MqttFixedHeader(buffer[0], bodyLength);
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
        
        private static int ReadBodyLength(IMqttChannel channel, byte initialEncodedByte, CancellationToken cancellationToken)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 128;
            var value = initialEncodedByte & 127;
            int encodedByte = initialEncodedByte;

            while ((encodedByte & 128) != 0)
            {
                // Here the async/await pattern is not used becuase the overhead of context switches
                // is too big for reading 1 byte in a row. We expect that the remaining data was sent
                // directly after the initial bytes. If the client disconnects just in this moment we
                // will get an exception anyway.
                encodedByte = ReadByteAsync(channel, cancellationToken).GetAwaiter().GetResult();

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }

                multiplier *= 128;
            }

            return value;
        }

        private static async Task<byte> ReadByteAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            var buffer = new byte[1];
            var readCount = await channel.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
            if (readCount <= 0)
            {
                ExceptionHelper.ThrowGracefulSocketClose();
            }

            return buffer[0];
        }
    }
}
