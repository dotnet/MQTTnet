using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Exceptions;

namespace MQTTnet.Serializer
{
    public static class MqttPacketReader
    {
        public static async Task<byte?> ReadFixedHeaderAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            // Wait for the next package which starts with the header. At this point there will probably
            // some large delay and thus the thread should be put back to the pool (await). So ReadByte()
            // is not an option here.
            var buffer = new byte[1];
            var readCount = await channel.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (readCount <= 0)
            {
                return null;
            }

            return buffer[0];
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

        public static byte[] ReadBytes(this Stream stream, int count)
        {
            var buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return buffer;
        }

        public static async Task<int> ReadBodyLengthAsync(IMqttChannel channel, CancellationToken cancellationToken)
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

                var readCount = await channel.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
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
