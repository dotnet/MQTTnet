using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Serializer;

namespace MQTTnet.Adapter
{
    public sealed class MqttChannelAdapter : IMqttChannelAdapter
    {
        private const uint ErrorOperationAborted = 0x800703E3;
        private const int ReadBufferSize = 4096;  // TODO: Move buffer size to config

        private readonly IMqttNetLogger _logger;
        private readonly IMqttChannel _channel;

        private bool _isDisposed;

        public MqttChannelAdapter(IMqttChannel channel, IMqttPacketSerializer serializer, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.Verbose<MqttChannelAdapter>("Connecting [Timeout={0}]", timeout);

            return ExecuteAndWrapExceptionAsync(() =>
                Internal.TaskExtensions.TimeoutAfter(ct => _channel.ConnectAsync(ct), timeout, cancellationToken));
        }

        public Task DisconnectAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();
            _logger.Verbose<MqttChannelAdapter>("Disconnecting [Timeout={0}]", timeout);

            return ExecuteAndWrapExceptionAsync(() =>
                Internal.TaskExtensions.TimeoutAfter(ct => _channel.DisconnectAsync(), timeout, CancellationToken.None));
        }

        public async Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, MqttBasePacket[] packets)
        {
            ThrowIfDisposed();

            foreach (var packet in packets)
            {
                if (packet == null)
                {
                    continue;
                }

                await SendPacketAsync(timeout, cancellationToken, packet).ConfigureAwait(false);
            }
        }

        private Task SendPacketAsync(TimeSpan timeout, CancellationToken cancellationToken, MqttBasePacket packet)
        {
            return ExecuteAndWrapExceptionAsync(() =>
            {
                _logger.Verbose<MqttChannelAdapter>("TX >>> {0} [Timeout={1}]", packet, timeout);

                var packetData = PacketSerializer.Serialize(packet);

                return Internal.TaskExtensions.TimeoutAfter(ct => _channel.WriteAsync(
                    packetData.Array,
                    packetData.Offset,
                    packetData.Count,
                    ct), timeout, cancellationToken);
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
                        receivedMqttPacket = await Internal.TaskExtensions.TimeoutAfter(ct => ReceiveAsync(_channel, ct), timeout, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        receivedMqttPacket = await ReceiveAsync(_channel, cancellationToken).ConfigureAwait(false);
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

        private static async Task<ReceivedMqttPacket> ReceiveAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            var header = await MqttPacketReader.ReadHeaderAsync(channel, cancellationToken).ConfigureAwait(false);
            if (header == null)
            {
                return null;
            }

            if (header.BodyLength == 0)
            {
                return new ReceivedMqttPacket(header, new MemoryStream(new byte[0], false));
            }

            var body = new MemoryStream(header.BodyLength);

            var buffer = new byte[Math.Min(ReadBufferSize, header.BodyLength)];
            while (body.Length < header.BodyLength)
            {
                var bytesLeft = header.BodyLength - (int)body.Length;
                if (bytesLeft > buffer.Length)
                {
                    bytesLeft = buffer.Length;
                }

                var readBytesCount = await channel.ReadAsync(buffer, 0, bytesLeft, cancellationToken).ConfigureAwait(false);

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
