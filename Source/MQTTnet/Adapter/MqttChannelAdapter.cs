// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Adapter
{
    public sealed class MqttChannelAdapter : Disposable, IMqttChannelAdapter
    {
        const uint ErrorOperationAborted = 0x800703E3;
        const int ReadBufferSize = 4096;

        readonly IMqttChannel _channel;
        readonly byte[] _fixedHeaderBuffer = new byte[2];
        readonly MqttNetSourceLogger _logger;

        readonly MqttPacketInspector _packetInspector;

        readonly byte[] _singleByteBuffer = new byte[1];

        readonly AsyncLock _syncRoot = new AsyncLock();

        Statistics _statistics;     // mutable struct, don't make readonly!

        public MqttChannelAdapter(IMqttChannel channel, MqttPacketFormatterAdapter packetFormatterAdapter, MqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _packetInspector = packetInspector;

            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource(nameof(MqttChannelAdapter));
        }

        public long BytesReceived => Volatile.Read(ref _statistics._bytesReceived);

        public long BytesSent => Volatile.Read(ref _statistics._bytesSent);

        public X509Certificate2 ClientCertificate => _channel.ClientCertificate;

        public string Endpoint => _channel.Endpoint;

        public bool IsReadingPacket { get; private set; }

        public bool IsSecureConnection => _channel.IsSecureConnection;

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            try
            {
                /*
                 * We have to implement a small workaround here to support connecting in Xamarin
                 * with a disabled WiFi network. If the WiFi is disabled the connect method will
                 * block forever. Even a cancellation token is not supported properly.
                 */

                var connectTask = _channel.ConnectAsync(cancellationToken);

                var timeout = new TaskCompletionSource<object>();
                using (cancellationToken.Register(() => timeout.TrySetResult(null)))
                {
                    await Task.WhenAny(connectTask, timeout.Task).ConfigureAwait(false);
                    if (timeout.Task.IsCompleted && !connectTask.IsCompleted)
                    {
                        throw new OperationCanceledException("MQTT connect canceled.", cancellationToken);
                    }

                    // Make sure that the exception from the connect task gets thrown.
                    await connectTask.ConfigureAwait(false);
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

        public async Task<MqttPacket> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            try
            {
                _packetInspector?.BeginReceivePacket();

                ReceivedMqttPacket receivedPacket;
                var receivedPacketTask = ReceiveAsync(cancellationToken);
                if (receivedPacketTask.IsCompleted)
                {
                    receivedPacket = receivedPacketTask.Result;
                }
                else
                {
                    receivedPacket = await receivedPacketTask.ConfigureAwait(false);
                }

                if (receivedPacket.TotalLength == 0 || cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                _packetInspector?.EndReceivePacket();

                Interlocked.Add(ref _statistics._bytesSent, receivedPacket.TotalLength);

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

        public void ResetStatistics() => _statistics.Reset();

        public async Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            // This lock makes sure that multiple threads can send packets at the same time.
            // This is required when a disconnect is sent from another thread while the 
            // worker thread is still sending publish packets etc.
            using (await _syncRoot.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                // Check for cancellation here again because "WaitAsync" might take some time.
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var packetBuffer = PacketFormatterAdapter.Encode(packet);
                    _packetInspector?.BeginSendPacket(packetBuffer);

                    _logger.Verbose("TX ({0} bytes) >>> {1}", packetBuffer.Length, packet);

                    if (packetBuffer.Payload.Count > 0)
                    {
                        await _channel.WriteAsync(packetBuffer.Packet, false, cancellationToken).ConfigureAwait(false);
                        await _channel.WriteAsync(packetBuffer.Payload, true, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await _channel.WriteAsync(packetBuffer.Packet, true, cancellationToken).ConfigureAwait(false);
                    }

                    Interlocked.Add(ref _statistics._bytesReceived, packetBuffer.Length);
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
                    PacketFormatterAdapter.Cleanup();
                }
            }
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

        async Task<int> ReadBodyLengthAsync(byte initialEncodedByte, CancellationToken cancellationToken)
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
                    return 0;
                }

                var readCount = await _channel.ReadAsync(_singleByteBuffer, 0, 1, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return 0;
                }

                if (readCount == 0)
                {
                    return 0;
                }

                _packetInspector?.FillReceiveBuffer(_singleByteBuffer);

                encodedByte = _singleByteBuffer[0];

                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            }

            return value;
        }

        async Task<ReadFixedHeaderResult> ReadFixedHeaderAsync(CancellationToken cancellationToken)
        {
            // The MQTT fixed header contains 1 byte of flags and at least 1 byte for the remaining data length.
            // So in all cases at least 2 bytes must be read for a complete MQTT packet.
            var buffer = _fixedHeaderBuffer;
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                // Check two times for cancellation because the call to _ReadAsync_ might block for some time.
                if (cancellationToken.IsCancellationRequested)
                {
                    return ReadFixedHeaderResult.Canceled;
                }

                int bytesRead;
                try
                {
                    bytesRead = await _channel.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return ReadFixedHeaderResult.Canceled;
                }
                catch (SocketException)
                {
                    return ReadFixedHeaderResult.ConnectionClosed;
                }
               
                if (cancellationToken.IsCancellationRequested)
                {
                    return ReadFixedHeaderResult.Canceled;
                }

                if (bytesRead == 0)
                {
                    return ReadFixedHeaderResult.ConnectionClosed;
                }

                totalBytesRead += bytesRead;
            }

            _packetInspector?.FillReceiveBuffer(buffer);

            var hasRemainingLength = buffer[1] != 0;
            if (!hasRemainingLength)
            {
                return new ReadFixedHeaderResult
                {
                    FixedHeader = new MqttFixedHeader(buffer[0], 0, totalBytesRead)
                };
            }

            var bodyLength = await ReadBodyLengthAsync(buffer[1], cancellationToken).ConfigureAwait(false);
            if (bodyLength == 0)
            {
                return new ReadFixedHeaderResult
                {
                    IsConnectionClosed = true
                };
            }

            totalBytesRead += bodyLength;
            return new ReadFixedHeaderResult
            {
                FixedHeader = new MqttFixedHeader(buffer[0], bodyLength, totalBytesRead)
            };
        }

        async Task<ReceivedMqttPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ReceivedMqttPacket.Empty;
            }

            var readFixedHeaderResult = await ReadFixedHeaderAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return ReceivedMqttPacket.Empty;
            }

            if (readFixedHeaderResult.IsConnectionClosed)
            {
                return ReceivedMqttPacket.Empty;
            }

            IsReadingPacket = true;
            try
            {
                var fixedHeader = readFixedHeaderResult.FixedHeader;
                if (fixedHeader.RemainingLength == 0)
                {
                    return new ReceivedMqttPacket(fixedHeader.Flags, EmptyBuffer.ArraySegment, 2);
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
                        return ReceivedMqttPacket.Empty;
                    }

                    if (readBytes == 0)
                    {
                        return ReceivedMqttPacket.Empty;
                    }

                    bodyOffset += readBytes;
                } while (bodyOffset < bodyLength);

                _packetInspector?.FillReceiveBuffer(body);

                var bodySegment = new ArraySegment<byte>(body, 0, bodyLength);
                return new ReceivedMqttPacket(fixedHeader.Flags, bodySegment, fixedHeader.TotalLength);
            }
            finally
            {
                IsReadingPacket = false;
            }
        }

        static bool WrapAndThrowException(Exception exception)
        {
            if (exception is OperationCanceledException || exception is MqttCommunicationTimedOutException || exception is MqttCommunicationException ||
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

        private struct Statistics
        {
            public long _bytesReceived;
            public long _bytesSent;

            public void Reset()
            {
                Volatile.Write(ref _bytesReceived, 0);
                Volatile.Write(ref _bytesSent, 0);
            }
        }
    }
}