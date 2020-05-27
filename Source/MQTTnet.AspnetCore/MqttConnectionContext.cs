﻿using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.AspNetCore.Extensions;
using MQTTnet.Internal;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttConnectionContext : IMqttChannelAdapter
    {
        readonly AsyncLock _writerLock = new AsyncLock();
        readonly SpanBasedMqttPacketBodyReader _reader;
        
        PipeReader _input;
        PipeWriter _output;
        
        public MqttConnectionContext(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection)
        {
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (Connection.Transport != null)
            {
                _input = Connection.Transport.Input;
                _output = Connection.Transport.Output;
            }

            _reader = new SpanBasedMqttPacketBodyReader();
        }

        public string Endpoint
        {
            get
            {
#if NETCOREAPP3_1
                if (Connection?.RemoteEndPoint != null)
                {
                    return Connection.RemoteEndPoint.ToString();
                }
#endif
                var connection = Http?.HttpContext?.Connection;
                if (connection == null)
                {
                    return Connection.ConnectionId;
                }

                return $"{connection.RemoteIpAddress}:{connection.RemotePort}";
            }
        }

        public bool IsSecureConnection => Http?.HttpContext?.Request?.IsHttps ?? false;

        public X509Certificate2 ClientCertificate => Http?.HttpContext?.Connection?.ClientCertificate;

        public ConnectionContext Connection { get; }
        
        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }

        public Action ReadingPacketStartedCallback { get; set; }
        public Action ReadingPacketCompletedCallback { get; set; }

        IHttpContextFeature Http => Connection.Features.Get<IHttpContextFeature>();

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Connection is TcpConnection tcp && !tcp.IsConnected)
            {
                await tcp.StartAsync().ConfigureAwait(false);
            }

            _input = Connection.Transport.Input;
            _output = Connection.Transport.Output;
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _input?.Complete();
            _output?.Complete();

            return Task.CompletedTask;
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var input = Connection.Transport.Input;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult readResult;
                    var readTask = input.ReadAsync(cancellationToken);
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
                            if (PacketFormatterAdapter.TryDecode(_reader, buffer, out var packet, out consumed, out observed, out var received))
                            {
                                BytesReceived += received;
                                return packet;
                            }
                            else
                            {
                                // we did receive something but the message is not yet complete
                                ReadingPacketStartedCallback?.Invoke();
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
                        input.AdvanceTo(consumed, observed);
                    }
                }
            }
            finally
            {
                ReadingPacketCompletedCallback?.Invoke();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public void ResetStatistics()
        {
            BytesReceived = 0;
            BytesSent = 0;
        }

        public async Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var formatter = PacketFormatterAdapter;
            using (await _writerLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var buffer = formatter.Encode(packet);
                var msg = buffer.AsMemory();
                var output = _output;
                var result = await output.WriteAsync(msg, cancellationToken).ConfigureAwait(false);
                if (result.IsCompleted)
                {
                    BytesSent += msg.Length;
                }
                PacketFormatterAdapter.FreeBuffer();
            }
        }

        public void Dispose()
        {
        }
    }
}
