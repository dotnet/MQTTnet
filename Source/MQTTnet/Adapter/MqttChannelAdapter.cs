using System;
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

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                _logger.Verbose("Connecting [Timeout={0}]", timeout);

                await Internal.TaskExtensions
                    .TimeoutAfterAsync(ct => _channel.ConnectAsync(ct), timeout, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (IsWrappedException(exception))
                {
                    throw;
                }

                WrapException(exception);
            }
        }

        public async Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                _logger.Verbose("Disconnecting [Timeout={0}]", timeout);

                await Internal.TaskExtensions
                    .TimeoutAfterAsync(ct => _channel.DisconnectAsync(), timeout, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (IsWrappedException(exception))
                {
                    throw;
                }

                WrapException(exception);
            }
        }

        public async Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("TX >>> {0}", packet);

                var packetData = PacketSerializer.Serialize(packet);

                await _channel.WriteAsync(packetData.Array, packetData.Offset, packetData.Count, cancellationToken).ConfigureAwait(false);

                PacketSerializer.FreeBuffer();
            }
            catch (Exception exception)
            {
                if (IsWrappedException(exception))
                {
                    throw;
                }

                WrapException(exception);
            }
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                ReceivedMqttPacket receivedMqttPacket;

                if (timeout > TimeSpan.Zero)
                {
                    receivedMqttPacket = await Internal.TaskExtensions.TimeoutAfterAsync(ct => ReceiveAsync(_channel, ct), timeout, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    receivedMqttPacket = await ReceiveAsync(_channel, cancellationToken).ConfigureAwait(false);
                }

                if (receivedMqttPacket == null || cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                var packet = PacketSerializer.Deserialize(receivedMqttPacket);
                if (packet == null)
                {
                    throw new MqttProtocolViolationException("Received malformed packet.");
                }

                _logger.Verbose("RX <<< {0}", packet);
                
                return packet;
            }
            catch (Exception exception)
            {
                if (IsWrappedException(exception))
                {
                    throw;
                }

                WrapException(exception);
            }

            return null;
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

                    // async/await is not used to avoid the overhead of context switches. We assume that the reamining data
                    // has been sent from the sender directly after the initial bytes.
                    var readBytes = channel.ReadAsync(body, bodyOffset, chunkSize, cancellationToken).GetAwaiter().GetResult();
                    if (readBytes <= 0)
                    {
                        ExceptionHelper.ThrowGracefulSocketClose();
                    }

                    bodyOffset += readBytes;
                } while (bodyOffset < body.Length);

                return new ReceivedMqttPacket(fixedHeader.Flags, new MqttPacketBodyReader(body, 0));
            }
            finally
            {
                ReadingPacketCompleted?.Invoke(this, EventArgs.Empty);
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

        private static bool IsWrappedException(Exception exception)
        {
            return exception is TaskCanceledException ||
                   exception is OperationCanceledException ||
                   exception is MqttCommunicationTimedOutException ||
                   exception is MqttCommunicationException;
        }

        private static void WrapException(Exception exception)
        {
            if (exception is IOException && exception.InnerException is SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    throw new OperationCanceledException();
                }
            }

            if (exception is COMException comException)
            {
                if ((uint)comException.HResult == ErrorOperationAborted)
                {
                    throw new OperationCanceledException();
                }
            }

            throw new MqttCommunicationException(exception);
        }
    }
}
