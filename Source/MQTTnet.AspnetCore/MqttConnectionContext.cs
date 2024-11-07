// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

public sealed class MqttConnectionContext : IMqttChannelAdapter
{
    readonly ConnectionContext _connection;
    readonly AsyncLock _writerLock = new();

    readonly PipeReader _input;
    readonly PipeWriter _output;

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
            return httpFeature?.HttpContext?.Connection.ClientCertificate;
        }
    }

    public string Endpoint
    {
        get
        {
            // mqtt over tcp
            if (_connection.RemoteEndPoint != null)
            {
                return _connection.RemoteEndPoint.ToString();
            }

            // mqtt over websocket
            var httpFeature = _connection.Features.Get<IHttpConnectionFeature>();
            if (httpFeature?.RemoteIpAddress != null)
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
            if (httpFeature?.HttpContext != null)
            {
                return httpFeature.HttpContext.Request.IsHttps;
            }

            return false;
        }
    }

    public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _input?.Complete();
        _output?.Complete();

        return Task.CompletedTask;
    }

    public void Dispose()
    { 
        _writerLock.Dispose();
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

                if (buffer.Payload.Length == 0)
                {
                    // zero copy
                    // https://github.com/dotnet/runtime/blob/e31ddfdc4f574b26231233dc10c9a9c402f40590/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs#L279
                    await _output.WriteAsync(buffer.Packet, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    WritePacketBuffer(_output, buffer);
                    await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

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
        int offset = buffer.Packet.Count;
        buffer.Payload.CopyTo(destination: span.Slice(offset));
        output.Advance(buffer.Length);
    }
}