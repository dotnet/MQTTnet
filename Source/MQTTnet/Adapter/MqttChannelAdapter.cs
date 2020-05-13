using MQTTnet.Channel;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Adapter
{
    public sealed class MqttChannelAdapter : Disposable, IMqttChannelAdapter
    {
        const uint _errorOperationAborted = 0x800703E3;
        const int _readBufferSize = 4096;  // TODO: Move buffer size to config

        readonly IMqttNetScopedLogger _logger;
        readonly IMqttChannel _channel;
        readonly MqttPacketReader _packetReader;

        readonly byte[] _fixedHeaderBuffer = new byte[2];

        SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);

        long _bytesReceived;
        long _bytesSent;

        public MqttChannelAdapter(IMqttChannel channel, MqttPacketFormatterAdapter packetFormatterAdapter, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));

            _packetReader = new MqttPacketReader(_channel);

            _logger = logger.CreateScopedLogger(nameof(MqttChannelAdapter));
        }

        public string Endpoint => _channel.Endpoint;

        public bool IsSecureConnection => _channel.IsSecureConnection;

        public X509Certificate2 ClientCertificate => _channel.ClientCertificate;

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent => Interlocked.Read(ref _bytesSent);
        public long BytesReceived => Interlocked.Read(ref _bytesReceived);

        public Action ReadingPacketStartedCallback { get; set; }
        public Action ReadingPacketCompletedCallback { get; set; }

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                if (timeout == TimeSpan.Zero)
                {
                    await _channel.ConnectAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await MqttTaskTimeout.WaitAsync(t => _channel.ConnectAsync(t), timeout, cancellationToken).ConfigureAwait(false);
                }
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
                if (timeout == TimeSpan.Zero)
                {
                    await _channel.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await MqttTaskTimeout.WaitAsync(
                        t => _channel.DisconnectAsync(t), timeout, cancellationToken).ConfigureAwait(false);
                }
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

        public async Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken)
        {
            await _writerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var packetData = PacketFormatterAdapter.Encode(packet);

                if (timeout == TimeSpan.Zero)
                {
                    await _channel.WriteAsync(packetData.Array, packetData.Offset, packetData.Count, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await MqttTaskTimeout.WaitAsync(
                        t => _channel.WriteAsync(packetData.Array, packetData.Offset, packetData.Count, t), timeout, cancellationToken).ConfigureAwait(false);
                }

                Interlocked.Add(ref _bytesReceived, packetData.Count);

                _logger.Verbose("TX ({0} bytes) >>> {1}", packetData.Count, packet);
            }
            catch (Exception exception)
            {
                if (IsWrappedException(exception))
                {
                    throw;
                }

                WrapException(exception);
            }
            finally
            {
                PacketFormatterAdapter.FreeBuffer();
                _writerSemaphore?.Release();
            }
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                ReceivedMqttPacket receivedMqttPacket;
                if (timeout == TimeSpan.Zero)
                {
                    receivedMqttPacket = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    receivedMqttPacket = await MqttTaskTimeout.WaitAsync(ReceiveAsync, timeout, cancellationToken).ConfigureAwait(false);
                }

                if (receivedMqttPacket == null || cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                Interlocked.Add(ref _bytesSent, receivedMqttPacket.TotalLength);

                if (PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.Unknown)
                {
                    PacketFormatterAdapter.DetectProtocolVersion(receivedMqttPacket);
                }

                var packet = PacketFormatterAdapter.Decode(receivedMqttPacket);
                if (packet == null)
                {
                    throw new MqttProtocolViolationException("Received malformed packet.");
                }

                _logger.Verbose("RX ({0} bytes) <<< {1}", receivedMqttPacket.TotalLength, packet);

                return packet;
            }
            catch (OperationCanceledException)
            {
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

        public void ResetStatistics()
        {
            Interlocked.Exchange(ref _bytesReceived, 0L);
            Interlocked.Exchange(ref _bytesSent, 0L);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _channel?.Dispose();

                _writerSemaphore?.Dispose();
                _writerSemaphore = null;
            }

            base.Dispose(disposing);
        }

        async Task<ReceivedMqttPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            var readFixedHeaderResult = await _packetReader.ReadFixedHeaderAsync(_fixedHeaderBuffer, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            try
            {
                if (readFixedHeaderResult.ConnectionClosed)
                {
                    return null;
                }

                ReadingPacketStartedCallback?.Invoke();

                var fixedHeader = readFixedHeaderResult.FixedHeader;
                if (fixedHeader.RemainingLength == 0)
                {
                    return new ReceivedMqttPacket(fixedHeader.Flags, null, 2);
                }

                var body = new byte[fixedHeader.RemainingLength];
                var bodyOffset = 0;
                var chunkSize = Math.Min(_readBufferSize, fixedHeader.RemainingLength);

                do
                {
                    var bytesLeft = body.Length - bodyOffset;
                    if (chunkSize > bytesLeft)
                    {
                        chunkSize = bytesLeft;
                    }

                    var readBytes = await _channel.ReadAsync(body, bodyOffset, chunkSize, cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    if (readBytes == 0)
                    {
                        return null;
                    }

                    bodyOffset += readBytes;
                } while (bodyOffset < body.Length);

                var bodyReader = new MqttPacketBodyReader(body, 0, body.Length);
                return new ReceivedMqttPacket(fixedHeader.Flags, bodyReader, fixedHeader.TotalLength);
            }
            finally
            {
                ReadingPacketCompletedCallback?.Invoke();
            }
        }

        static bool IsWrappedException(Exception exception)
        {
            return exception is OperationCanceledException ||
                   exception is MqttCommunicationTimedOutException ||
                   exception is MqttCommunicationException;
        }

        static void WrapException(Exception exception)
        {
            if (exception is IOException && exception.InnerException is SocketException innerException)
            {
                exception = innerException;
            }

            if (exception is SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                    socketException.SocketErrorCode == SocketError.OperationAborted)
                {
                    throw new OperationCanceledException();
                }
            }

            if (exception is COMException comException)
            {
                if ((uint)comException.HResult == _errorOperationAborted)
                {
                    throw new OperationCanceledException();
                }
            }

            throw new MqttCommunicationException(exception);
        }
    }
}
