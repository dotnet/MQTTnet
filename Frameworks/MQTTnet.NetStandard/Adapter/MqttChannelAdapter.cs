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
    public sealed class MqttChannelAdapter : IMqttChannelAdapter
    {
        private const uint ErrorOperationAborted = 0x800703E3;
        private const int ReadBufferSize = 4096;  // TODO: Move buffer size to config

        private bool _isDisposed;
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

        public Task ConnectAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();
            _logger.Verbose<MqttChannelAdapter>("Connecting [Timeout={0}]", timeout);

            return ExecuteAndWrapExceptionAsync(() => _channel.ConnectAsync().TimeoutAfter(timeout));
        }

        public Task DisconnectAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();
            _logger.Verbose<MqttChannelAdapter>("Disconnecting [Timeout={0}]", timeout);

            return ExecuteAndWrapExceptionAsync(() => _channel.DisconnectAsync().TimeoutAfter(timeout));
        }

        public Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<MqttBasePacket> packets)
        {
            ThrowIfDisposed();

            return ExecuteAndWrapExceptionAsync(async () =>
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    foreach (var packet in packets)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (packet == null)
                        {
                            continue;
                        }

                        _logger.Verbose<MqttChannelAdapter>("TX >>> {0} [Timeout={1}]", packet, timeout);

                        var chunks = PacketSerializer.Serialize(packet);
                        foreach (var chunk in chunks)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            await _channel.SendStream.WriteAsync(chunk.Array, chunk.Offset, chunk.Count, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
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
            ThrowIfDisposed();

            MqttBasePacket packet = null;
            await ExecuteAndWrapExceptionAsync(async () =>
            {
                ReceivedMqttPacket receivedMqttPacket = null;
                try
                {

                    if (timeout > TimeSpan.Zero)
                    {
                        var timeoutCts = new CancellationTokenSource(timeout);
                        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                        receivedMqttPacket = await ReceiveAsync(_channel.ReceiveStream, linkedCts.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        receivedMqttPacket = await ReceiveAsync(_channel.ReceiveStream, cancellationToken).ConfigureAwait(false);
                    }

                    if (receivedMqttPacket == null || cancellationToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    packet = PacketSerializer.Deserialize(receivedMqttPacket.Header, receivedMqttPacket.Body);
                    if (packet == null)
                    {
                        throw new MqttProtocolViolationException("Received malformed packet.");
                    }

                    _logger.Verbose<MqttChannelAdapter>("RX <<< {0}", packet);
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
            var header = await MqttPacketReader.ReadHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            if (header == null)
            {
                return null;
            }

            if (header.BodyLength == 0)
            {
                return new ReceivedMqttPacket(header, new MemoryStream(new byte[0], false));
            }

            var body = header.BodyLength <= ReadBufferSize ? new MemoryStream(header.BodyLength) : new MemoryStream();

            var buffer = new byte[ReadBufferSize];
            while (body.Length < header.BodyLength)
            {
                var bytesLeft = header.BodyLength - (int)body.Length;
                if (bytesLeft > buffer.Length)
                {
                    bytesLeft = buffer.Length;
                }

                var readBytesCount = await stream.ReadAsync(buffer, 0, bytesLeft, cancellationToken).ConfigureAwait(false);

                // Check if the client closed the connection before sending the full body.
                if (readBytesCount == 0)
                {
                    throw new MqttCommunicationException("Connection closed while reading remaining packet body.");
                }

                // Here is no need to await because internally only an array is used and no real I/O operation is made.
                // Using async here will only generate overhead.
                body.Write(buffer, 0, readBytesCount);
            }

            body.Seek(0L, SeekOrigin.Begin);

            return new ReceivedMqttPacket(header, body);
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

        public void Dispose()
        {
            _isDisposed = true;
            _semaphore?.Dispose();
            _channel?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MqttChannelAdapter));
            }
        }
    }
}
