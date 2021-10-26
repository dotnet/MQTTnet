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
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Adapter
{
    public sealed class MqttChannelAdapter : Disposable, IMqttChannelAdapter
    {
        const uint ErrorOperationAborted = 0x800703E3;
        const int ReadBufferSize = 4096;

        readonly byte[] _singleByteBuffer = new byte[1];
        readonly byte[] _fixedHeaderBuffer = new byte[2];

        readonly MqttPacketInspectorHandler _packetInspectorHandler;
        readonly MqttNetSourceLogger _logger;
        readonly IMqttChannel _channel;

        readonly AsyncLock _syncRoot = new AsyncLock();

        long _bytesReceived;
        long _bytesSent;

        public MqttChannelAdapter(IMqttChannel channel, MqttPacketFormatterAdapter packetFormatterAdapter, IMqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));

            _packetInspectorHandler = new MqttPacketInspectorHandler(packetInspector, logger);

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttChannelAdapter));
        }

        public string Endpoint => _channel.Endpoint;

        public bool IsSecureConnection => _channel.IsSecureConnection;

        public X509Certificate2 ClientCertificate => _channel.ClientCertificate;

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent => Interlocked.Read(ref _bytesSent);

        public long BytesReceived => Interlocked.Read(ref _bytesReceived);

        public bool IsReadingPacket { get; private set; }

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                if (!WrapAndThrowException(exception))
                {
                    throw;
                }
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            try
            {
                await _channel.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (!WrapAndThrowException(exception))
                {
                    throw;
                }
            }
        }

        public async Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            using (await _syncRoot.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Check for cancellation here again because "WaitAsync" might take some time.
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var packetData = PacketFormatterAdapter.Encode(packet);
                    _packetInspectorHandler.BeginSendPacket(packetData);

                    await packetData.WriteToAsync(_channel, cancellationToken).ConfigureAwait(false);
                    
                    Interlocked.Add(ref _bytesReceived, packetData.Length);

                    _logger.Verbose("TX ({0} bytes) >>> {1}", packetData.Length, packet);
                }
                catch (Exception exception)
                {
                    if (!WrapAndThrowException(exception))
                    {
                        throw;
                    }
                }
                finally
                {
                    PacketFormatterAdapter.FreeBuffer();
                }
            }
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            try
            {
                _packetInspectorHandler.BeginReceivePacket();

                var receivedPacket = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (receivedPacket == null || cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                _packetInspectorHandler.EndReceivePacket();

                Interlocked.Add(ref _bytesSent, receivedPacket.TotalLength);

                if (PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.Unknown)
                {
                    PacketFormatterAdapter.DetectProtocolVersion(receivedPacket);
                }

                var packet = PacketFormatterAdapter.Decode(receivedPacket);
                if (packet == null)
                {
                    throw new MqttProtocolViolationException("Received malformed packet.");
                }

                _logger.Verbose("RX ({0} bytes) <<< {1}", receivedPacket.TotalLength, packet);

                return packet;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                if (!WrapAndThrowException(exception))
                {
                    throw;
                }
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
                _channel.Dispose();
                _syncRoot.Dispose();
            }

            base.Dispose(disposing);
        }

        async Task<ReceivedMqttPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var readFixedHeaderResult = await ReadFixedHeaderAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (readFixedHeaderResult.ConnectionClosed)
            {
                return null;
            }

            try
            {
                IsReadingPacket = true;

                var fixedHeader = readFixedHeaderResult.FixedHeader;
                if (fixedHeader.RemainingLength == 0)
                {
                    return new ReceivedMqttPacket(fixedHeader.Flags, new MqttPacketBodyReader(new byte[0], 0, 0), 2);
                }

                var bodyLength = fixedHeader.RemainingLength;
                var body = new byte[bodyLength];

                var bodyOffset = 0;
                var chunkSize = Math.Min(ReadBufferSize, bodyLength);

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
                } while (bodyOffset < bodyLength);

                _packetInspectorHandler.FillReceiveBuffer(body);

                var bodyReader = new MqttPacketBodyReader(body, 0, bodyLength);
                return new ReceivedMqttPacket(fixedHeader.Flags, bodyReader, fixedHeader.TotalLength);
            }
            finally
            {
                IsReadingPacket = false;
            }
        }

        async Task<ReadFixedHeaderResult> ReadFixedHeaderAsync(CancellationToken cancellationToken)
        {
            // The MQTT fixed header contains 1 byte of flags and at least 1 byte for the remaining data length.
            // So in all cases at least 2 bytes must be read for a complete MQTT packet.
            var buffer = _fixedHeaderBuffer;
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await _channel.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (bytesRead == 0)
                {
                    return new ReadFixedHeaderResult
                    {
                        ConnectionClosed = true
                    };
                }

                totalBytesRead += bytesRead;
            }

            _packetInspectorHandler.FillReceiveBuffer(buffer);

            var hasRemainingLength = buffer[1] != 0;
            if (!hasRemainingLength)
            {
                return new ReadFixedHeaderResult
                {
                    FixedHeader = new MqttFixedHeader(buffer[0], 0, totalBytesRead)
                };
            }

            var bodyLength = await ReadBodyLengthAsync(buffer[1], cancellationToken).ConfigureAwait(false);

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

        async Task<int?> ReadBodyLengthAsync(byte initialEncodedByte, CancellationToken cancellationToken)
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

                var readCount = await _channel.ReadAsync(_singleByteBuffer, 0, 1, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (readCount == 0)
                {
                    return null;
                }

                _packetInspectorHandler.FillReceiveBuffer(_singleByteBuffer);

                encodedByte = _singleByteBuffer[0];

                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            }

            return value;
        }
        
        static bool WrapAndThrowException(Exception exception)
        {
            if (exception is OperationCanceledException ||
                exception is MqttCommunicationTimedOutException ||
                exception is MqttCommunicationException ||
                exception is MqttProtocolViolationException)
            {
                return false;
            }
            
            if (exception is IOException && exception.InnerException is SocketException innerException)
            {
                exception = innerException;
            }

            if (exception is SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.OperationAborted)
                {
                    throw new OperationCanceledException();
                }

                if (socketException.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    throw new MqttCommunicationException(socketException);
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
