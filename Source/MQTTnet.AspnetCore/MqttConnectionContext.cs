// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using System;
using System.IO.Pipelines;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttConnectionContext : IMqttChannelAdapter
    {
        readonly AsyncLock _writerLock = new AsyncLock();
        readonly ConnectionContext _connection;
        PipeReader _input;
        PipeWriter _output;

        public MqttConnectionContext(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection)
        {
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _input = connection.Transport.Input;
            _output = connection.Transport.Output;
        }

        public long BytesReceived { get; private set; }

        public long BytesSent { get; private set; }

        public X509Certificate2 ClientCertificate
        {
            get
            {
                // mqtt over tcp
                var tlsFeature = _connection.Features.Get<ITlsConnectionFeature>();
                if (tlsFeature != null)
                {
                    return tlsFeature.ClientCertificate;
                }

                // mqtt over websocket
                var httpFeature = _connection.Features.Get<IHttpContextFeature>();
                if (httpFeature != null && httpFeature.HttpContext != null)
                {
                    return httpFeature.HttpContext.Connection.ClientCertificate;
                }
                return null;
            }
        }


        public string Endpoint
        {
            get
            {
                // mqtt over tcp 
#if NETCOREAPP3_1_OR_GREATER
                if (_connection.RemoteEndPoint != null)
                {
                    return _connection.RemoteEndPoint.ToString();
                }
#endif
                // mqtt over websocket
                var httpFeature = _connection.Features.Get<IHttpConnectionFeature>();
                if (httpFeature != null && httpFeature.RemoteIpAddress != null)
                {
                    return new IPEndPoint(httpFeature.RemoteIpAddress, httpFeature.RemotePort).ToString();
                }

                return null;
            }
        }

        public bool IsSecureConnection
        {
            get
            {
                // mqtt over tcp
                var tlsFeature = _connection.Features.Get<ITlsConnectionFeature>();
                if (tlsFeature != null)
                {
                    return true;
                }

                // mqtt over websocket
                var httpFeature = _connection.Features.Get<IHttpContextFeature>();
                if (httpFeature != null && httpFeature.HttpContext != null)
                {
                    return httpFeature.HttpContext.Request.IsHttps;
                }
                return false;
            }
        }


        public bool IsReadingPacket { get; private set; }

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }


        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_connection is TcpConnection tcp && !tcp.IsConnected)
            {
                await tcp.StartAsync().ConfigureAwait(false);
            }

            _input = _connection.Transport.Input;
            _output = _connection.Transport.Output;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            _input?.Complete();
            _output?.Complete();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public async Task<MqttPacket> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult readResult;
                    var readTask = _input.ReadAsync(cancellationToken);
                    if (readTask.IsCompleted)
                    {
                        readResult = readTask.Result;
                    }
                    else
                    {
                        readResult = await readTask.ConfigureAwait(false);
                    }

                    var buffer = readResult.Buffer;

                    var consumed = buffer.Start;
                    var observed = buffer.Start;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            if (PacketFormatterAdapter.TryDecode(buffer, out var packet, out consumed, out observed, out var received))
                            {
                                BytesReceived += received;
                                return packet;
                            }
                            else
                            {
                                // we did receive something but the message is not yet complete
                                IsReadingPacket = true;
                            }
                        }
                        else if (readResult.IsCompleted)
                        {
                            throw new MqttCommunicationException("Connection Aborted");
                        }
                    }
                    finally
                    {
                        // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                        // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                        // before yielding the read again.
                        _input.AdvanceTo(consumed, observed);
                    }
                }
            }
            catch (Exception exception)
            {
                // completing the channel makes sure that there is no more data read after a protocol error
                _input?.Complete(exception);
                _output?.Complete(exception);
                throw;
            }
            finally
            {
                IsReadingPacket = false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public void ResetStatistics()
        {
            BytesReceived = 0;
            BytesSent = 0;
        }

        public async Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            using (await _writerLock.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    var buffer = PacketFormatterAdapter.Encode(packet);

                    WritePacketBuffer(_output, buffer);
                    await _output.FlushAsync(cancellationToken).ConfigureAwait(false);

                    BytesSent += buffer.Length;
                }
                finally
                {
                    PacketFormatterAdapter.Cleanup();
                }
            }
        }

        static void WritePacketBuffer(PipeWriter output, MqttPacketBuffer buffer)
        {
            // copy MqttPacketBuffer's Packet and Payload to the same buffer block of PipeWriter
            // MqttPacket will be transmitted within the bounds of a WebSocket frame after PipeWriter.FlushAsync

            var span = output.GetSpan(buffer.Length);

            buffer.Packet.AsSpan().CopyTo(span);
            buffer.Payload.AsSpan().CopyTo(span.Slice(buffer.Packet.Count));

            output.Advance(buffer.Length);
        }
    }
}