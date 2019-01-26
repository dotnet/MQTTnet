using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Exceptions;
using MQTTnet.Internal;

namespace MQTTnet.Formatter
{
    public class MqttPacketReader
    {
        private readonly byte[] _singleByteBuffer = new byte[1];

        private readonly IMqttChannel _channel;

        public MqttPacketReader(IMqttChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public async Task<ReadFixedHeaderResult> ReadFixedHeaderAsync(byte[] fixedHeaderBuffer, CancellationToken cancellationToken)
        {
            // The MQTT fixed header contains 1 byte of flags and at least 1 byte for the remaining data length.
            // So in all cases at least 2 bytes must be read for a complete MQTT packet.
            // async/await is used here because the next packet is received in a couple of minutes so the performance
            // impact is acceptable according to a useless waiting thread.
            var buffer = fixedHeaderBuffer;
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await _channel.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (bytesRead == 0)
                {
                    return new ReadFixedHeaderResult
                    {
                        ConnectionClosed = true
                    };
                }
                
                totalBytesRead += bytesRead;
            }

            var hasRemainingLength = buffer[1] != 0;
            if (!hasRemainingLength)
            {
                return new ReadFixedHeaderResult
                {
                    FixedHeader = new MqttFixedHeader(buffer[0], 0, totalBytesRead)
                };
            }

#if WINDOWS_UWP
            // UWP will have a dead lock when calling this not async.
            var bodyLength = await ReadBodyLengthAsync(buffer[1], cancellationToken).ConfigureAwait(false);
#else
            // Here the async/await pattern is not used because the overhead of context switches
            // is too big for reading 1 byte in a row. We expect that the remaining data was sent
            // directly after the initial bytes. If the client disconnects just in this moment we
            // will get an exception anyway.
            var bodyLength = ReadBodyLength(buffer[1], cancellationToken);
#endif

            if (!bodyLength.HasValue)
            {
                return new ReadFixedHeaderResult
                {
                    ConnectionClosed = true
                };
            }

            totalBytesRead += bodyLength.Value;
            return new ReadFixedHeaderResult
            {
                FixedHeader = new MqttFixedHeader(buffer[0], bodyLength.Value, totalBytesRead)
            };
        }

#if !WINDOWS_UWP
        private int? ReadBodyLength(byte initialEncodedByte, CancellationToken cancellationToken)
        {
            var offset = 0;
            var multiplier = 128;
            var value = initialEncodedByte & 127;
            int encodedByte = initialEncodedByte;

            while ((encodedByte & 128) != 0)
            {
                offset++;
                if (offset > 3)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                var buffer = ReadByte(cancellationToken);
                if (!buffer.HasValue)
                {
                    return null;
                }

                encodedByte = buffer.Value;

                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            }

            return value;
        }

        private byte? ReadByte(CancellationToken cancellationToken)
        {
            var readCount = _channel.ReadAsync(_singleByteBuffer, 0, 1, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (readCount == 0)
            {
                return null;
            }

            return _singleByteBuffer[0];
        }

#else
        
        private async Task<int?> ReadBodyLengthAsync(byte initialEncodedByte, CancellationToken cancellationToken)
        {
            var offset = 0;
            var multiplier = 128;
            var value = initialEncodedByte & 127;
            int encodedByte = initialEncodedByte;

            while ((encodedByte & 128) != 0)
            {
                offset++;
                if (offset > 3)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                var buffer = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (!buffer.HasValue)
                {
                    return null;
                }

                encodedByte = buffer.Value;

                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            }

            return value;
        }

        private async Task<byte?> ReadByteAsync(CancellationToken cancellationToken)
        {
            var readCount = await _channel.ReadAsync(_singleByteBuffer, 0, 1, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (readCount == 0)
            {
                return null;
            }

            return _singleByteBuffer[0];
        }

#endif
    }
}
