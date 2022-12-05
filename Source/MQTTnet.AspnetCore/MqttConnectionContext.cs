// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using System;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttConnectionContext : IMqttChannelAdapter
    {
        readonly AsyncLock _writerLock = new AsyncLock();
        PipeReader _input;
        PipeWriter _output;

        public MqttConnectionContext(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection)
        {
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _input = Connection.Transport.Input;
            _output = Connection.Transport.Output;
        }

        public long BytesReceived { get; private set; }

        public long BytesSent { get; private set; }

        public X509Certificate2 ClientCertificate => Http?.HttpContext?.Connection?.ClientCertificate;

        public ConnectionContext Connection { get; }

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

        public bool IsReadingPacket { get; private set; }

        public bool IsSecureConnection => Http?.HttpContext?.Request.IsHttps ?? false;

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        IHttpContextFeature Http => Connection.Features.Get<IHttpContextFeature>();

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (Connection is TcpConnection tcp && !tcp.IsConnected)
            {
                await tcp.StartAsync().ConfigureAwait(false);
            }

            _input = Connection.Transport.Input;
            _output = Connection.Transport.Output;
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
                        input.AdvanceTo(consumed, observed);
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

        /// <summary>
        /// copy MqttPacketBuffer's Packet and Payload to the same buffer block of PipeWriter
        /// MqttPacket will be transmitted within the bounds of a WebSocket frame after PipeWriter.FlushAsync
        /// </summary>
        /// <param name="output"></param>
        /// <param name="buffer"></param>
        private static void WritePacketBuffer(PipeWriter output, MqttPacketBuffer buffer)
        {
            var span = output.GetSpan(buffer.Length);

            buffer.Packet.AsSpan().CopyTo(span);
            buffer.Payload.AsSpan().CopyTo(span.Slice(buffer.Packet.Count));

            output.Advance(buffer.Length);
        }
    }
}