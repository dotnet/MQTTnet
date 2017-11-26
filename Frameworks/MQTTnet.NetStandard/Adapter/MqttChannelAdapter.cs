using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Serializer;

namespace MQTTnet.Adapter
{
    public class MqttChannelAdapter : IMqttChannelAdapter
    {
        private const uint ErrorOperationAborted = 0x800703E3;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IMqttNetLogger _logger;
        private readonly IMqttChannel _channel;

        public MqttChannelAdapter(IMqttChannel channel, IMqttPacketSerializer serializer, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public async Task ConnectAsync(TimeSpan timeout)
        {
            _logger.Info<MqttChannelAdapter>("Connecting [Timeout={0}]", timeout);

            await ExecuteAndWrapExceptionAsync(() => _channel.ConnectAsync().TimeoutAfter(timeout));
        }

        public async Task DisconnectAsync(TimeSpan timeout)
        {
            _logger.Info<MqttChannelAdapter>("Disconnecting [Timeout={0}]", timeout);

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

                        _logger.Trace<MqttChannelAdapter>("TX >>> {0} [Timeout={1}]", packet, timeout);

                        var chunks = PacketSerializer.Serialize(packet);
                        foreach (var chunk in chunks)
                        {
                            await _channel.SendStream.WriteAsync(chunk.Array, chunk.Offset, chunk.Count, cancellationToken).ConfigureAwait(false);
                        }
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

                    _logger.Trace<MqttChannelAdapter>("RX <<< {0}", packet);
                }
                finally
                {
                    receivedMqttPacket?.Dispose();
                }
            }).ConfigureAwait(false);

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
            
            return new ReceivedMqttPacket(header, new MemoryStream(body, 0, body.Length, false, true));
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
                if ((uint) comException.HResult == ErrorOperationAborted)
                {
                    throw new OperationCanceledException();
                }

                throw new MqttCommunicationException(comException);
            }
            catch (IOException exception)
            {
                if (exception.InnerException is SocketException socketException)
                {
                    if (socketException.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        throw new OperationCanceledException();
                    }
                }

                throw new MqttCommunicationException(exception);
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }
    }
}
