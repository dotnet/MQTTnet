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
        private const int ReadBufferSize = 4096;  // TODO: Move buffer size to config

        private readonly IMqttNetChildLogger _logger;
        private readonly IMqttChannel _channel;

        private bool _isDisposed;

        public MqttChannelAdapter(IMqttChannel channel, IMqttPacketSerializer serializer, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _logger = logger.CreateChildLogger(nameof(MqttChannelAdapter));
        }

        public string Endpoint => _channel.Endpoint;

        public IMqttPacketSerializer PacketSerializer { get; }

        public event EventHandler ReadingPacketStarted;
        public event EventHandler ReadingPacketCompleted;

        public Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.Verbose("Connecting [Timeout={0}]", timeout);

            return ExecuteAndWrapExceptionAsync(() =>
                Internal.TaskExtensions.TimeoutAfter(ct => _channel.ConnectAsync(ct), timeout, cancellationToken));
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.Verbose("Disconnecting [Timeout={0}]", timeout);

            return ExecuteAndWrapExceptionAsync(() =>
                Internal.TaskExtensions.TimeoutAfter(ct => _channel.DisconnectAsync(), timeout, cancellationToken));
        }

        public async Task SendPacketsAsync(TimeSpan timeout, IEnumerable<MqttBasePacket> packets, CancellationToken cancellationToken)
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
                _logger.Verbose("TX >>> {0} [Timeout={1}]", packet, timeout);

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
                ReceivedMqttPacket receivedMqttPacket;

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
                    return;
                }

                packet = PacketSerializer.Deserialize(receivedMqttPacket);
                if (packet == null)
                {
                    throw new MqttProtocolViolationException("Received malformed packet.");
                }

                _logger.Verbose("RX <<< {0}", packet);
            }).ConfigureAwait(false);

            return packet;
        }

        private async Task<ReceivedMqttPacket> ReceiveAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            var fixedHeader = await MqttPacketReader.ReadFixedHeaderAsync(channel, cancellationToken).ConfigureAwait(false);
            if (fixedHeader == null)
            {
                return null;
            }

            try
            {
                ReadingPacketStarted?.Invoke(this, EventArgs.Empty);

                if (fixedHeader.RemainingLength == 0)
                {
                    return new ReceivedMqttPacket(fixedHeader.Flags, null);
                }

                var body = new byte[fixedHeader.RemainingLength];
                var bodyOffset = 0;
                var chunkSize = Math.Min(ReadBufferSize, fixedHeader.RemainingLength);

                do
                {
                    var bytesLeft = body.Length - bodyOffset;
                    if (chunkSize > bytesLeft)
                    {
                        chunkSize = bytesLeft;
                    }

                    var readBytes = await channel.ReadAsync(body, bodyOffset, chunkSize, cancellationToken) .ConfigureAwait(false);
                    if (readBytes <= 0)
                    {
                        ExceptionHelper.ThrowGracefulSocketClose();
                    }

                    bodyOffset += readBytes;
                } while (bodyOffset < body.Length);

                return new ReceivedMqttPacket(fixedHeader.Flags, new MqttPacketBodyReader(body));
            }
            finally
            {
                ReadingPacketCompleted?.Invoke(this, EventArgs.Empty);
            }
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
