using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Exceptions;
using MQTTnet.Internal;

namespace MQTTnet.Serializer
{
    public static class MqttPacketReader
    {
        [ThreadStatic]
        private static byte[] _fixedHeaderBuffer;

        [ThreadStatic]
        private static byte[] _singleByteBuffer;

        public static async Task<MqttFixedHeader> ReadFixedHeaderAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            // The MQTT fixed header contains 1 byte of flags and at least 1 byte for the remaining data length.
            // So in all cases at least 2 bytes must be read for a complete MQTT packet.
            // async/await is used here because the next packet is received in a couple of minutes so the performance
            // impact is acceptable according to a useless waiting thread.
            var buffer = InitializeFixedHeaderBuffer();
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await channel.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
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
                encodedByte = ReadByte(channel, cancellationToken);

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }

                multiplier *= 128;
            }

            return value;
        }

        private static byte ReadByte(IMqttChannel channel, CancellationToken cancellationToken)
        {
            var buffer = InitializeSingleByteBuffer();
            var readCount = channel.ReadAsync(buffer, 0, 1, cancellationToken).GetAwaiter().GetResult();
            if (readCount <= 0)
            {
                ExceptionHelper.ThrowGracefulSocketClose();
            }

            return buffer[0];
        }

        private static byte[] InitializeFixedHeaderBuffer()
        {
            return _fixedHeaderBuffer ?? (_fixedHeaderBuffer = new byte[2]);
        }

        private static byte[] InitializeSingleByteBuffer()
        {
            return _singleByteBuffer ?? (_singleByteBuffer = new byte[1]);
        }
    }
}
