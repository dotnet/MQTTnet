using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Core.Adapter
{
    public class MqttChannelCommunicationAdapter : IMqttCommunicationAdapter
    {
        private const uint ErrorOperationAborted = 0x800703E3;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger<MqttChannelCommunicationAdapter> _logger;
        private readonly IMqttCommunicationChannel _channel;

        public MqttChannelCommunicationAdapter(IMqttCommunicationChannel channel, IMqttPacketSerializer serializer, ILogger<MqttChannelCommunicationAdapter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public async Task ConnectAsync(TimeSpan timeout)
        {
            await ExecuteAndWrapExceptionAsync(() => _channel.ConnectAsync().TimeoutAfter(timeout));
        }

        public async Task DisconnectAsync(TimeSpan timeout)
        {
            await ExecuteAndWrapExceptionAsync(() => _channel.DisconnectAsync().TimeoutAfter(timeout));
        }

        public async Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<MqttBasePacket> packets)
        {
            await ExecuteAndWrapExceptionAsync(async () =>
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    foreach (var packet in packets)
                    {
                        if (packet == null)
                        {
                            continue;
                        }

                        _logger.LogInformation("TX >>> {0} [Timeout={1}]", packet, timeout);

                        var writeBuffer = PacketSerializer.Serialize(packet);
                        await _channel.SendStream.WriteAsync(writeBuffer, 0, writeBuffer.Length, cancellationToken).ConfigureAwait(false);
                    }

                    if (timeout > TimeSpan.Zero)
                    {
                        await _channel.SendStream.FlushAsync(cancellationToken).TimeoutAfter(timeout).ConfigureAwait(false);
                    }
                    else
                    {
                        await _channel.SendStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            MqttBasePacket packet = null;
            await ExecuteAndWrapExceptionAsync(async () =>
            {
                ReceivedMqttPacket receivedMqttPacket = null;
                try
                {
                    if (timeout > TimeSpan.Zero)
                    {
                        receivedMqttPacket = await ReceiveAsync(_channel.ReceiveStream, cancellationToken).TimeoutAfter(timeout).ConfigureAwait(false);
                    }
                    else
                    {
                        receivedMqttPacket = await ReceiveAsync(_channel.ReceiveStream, cancellationToken).ConfigureAwait(false);
                    }

                    if (receivedMqttPacket == null || cancellationToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    packet = PacketSerializer.Deserialize(receivedMqttPacket);
                    if (packet == null)
                    {
                        throw new MqttProtocolViolationException("Received malformed packet.");
                    }

                    _logger.LogInformation("RX <<< {0}", packet);
                }
                finally
                {
                    receivedMqttPacket?.Dispose();
                }
            });

            return packet;
        }

        private static async Task<ReceivedMqttPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken)
        {
            var header = MqttPacketReader.ReadHeaderFromSource(stream, cancellationToken);
            if (header == null)
            {
                return null;
            }

            if (header.BodyLength == 0)
            {
                return new ReceivedMqttPacket(header, new MemoryStream(0));
            }

            var body = new byte[header.BodyLength];

            var offset = 0;
            do
            {
                var readBytesCount = await stream.ReadAsync(body, offset, body.Length - offset, cancellationToken).ConfigureAwait(false);
                offset += readBytesCount;
            } while (offset < header.BodyLength);

            return new ReceivedMqttPacket(header, new MemoryStream(body, 0, body.Length));
        }

        private static async Task ExecuteAndWrapExceptionAsync(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MqttCommunicationTimedOutException)
            {
                throw;
            }
            catch (MqttCommunicationException)
            {
                throw;
            }
            catch (COMException comException)
            {
                if ((uint)comException.HResult == ErrorOperationAborted)
                {
                    throw new OperationCanceledException();
                }

                throw new MqttCommunicationException(comException);
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }
    }
}
