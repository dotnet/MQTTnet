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

class MqttChannel : IDisposable
{
    readonly AsyncLock _writerLock = new();
    readonly PipeReader _input;
    readonly PipeWriter _output;
    readonly MqttPacketInspector? _packetInspector;
    readonly bool _serverModeWebSocket;

    public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

    public long BytesReceived { get; private set; }

    public long BytesSent { get; private set; }

    public X509Certificate2? ClientCertificate { get; }

    public string? Endpoint { get; }

    public bool IsSecureConnection { get; }


    public MqttChannel(
        MqttPacketFormatterAdapter packetFormatterAdapter,
        ConnectionContext connection,
        MqttPacketInspector? packetInspector = null)
    {
        PacketFormatterAdapter = packetFormatterAdapter;
        _packetInspector = packetInspector;

        var httpContextFeature = connection.Features.Get<IHttpContextFeature>();
        var tlsConnectionFeature = connection.Features.Get<ITlsConnectionFeature>();
        Endpoint = GetRemoteEndPoint(httpContextFeature, connection.RemoteEndPoint);
        IsSecureConnection = IsTlsConnection(httpContextFeature, tlsConnectionFeature);
        ClientCertificate = GetClientCertificate(httpContextFeature, tlsConnectionFeature);
        _serverModeWebSocket = IsServerModeWebSocket(httpContextFeature);

        _input = connection.Transport.Input;
        _output = connection.Transport.Output;
    }

    private static bool IsServerModeWebSocket(IHttpContextFeature? _httpContextFeature)
    {
        return _httpContextFeature != null && _httpContextFeature.HttpContext != null && _httpContextFeature.HttpContext.WebSockets.IsWebSocketRequest;
    }


    private static string? GetRemoteEndPoint(IHttpContextFeature? _httpContextFeature, EndPoint? remoteEndPoint)
    {
        if (_httpContextFeature != null && _httpContextFeature.HttpContext != null)
        {
            var httpConnection = _httpContextFeature.HttpContext.Connection;
            var remoteAddress = httpConnection.RemoteIpAddress;
            return remoteAddress == null ? null : $"{remoteAddress}:{httpConnection.RemotePort}";
        }

        return remoteEndPoint is DnsEndPoint dnsEndPoint
            ? $"{dnsEndPoint.Host}:{dnsEndPoint.Port}"
            : remoteEndPoint?.ToString();
    }

    private static bool IsTlsConnection(IHttpContextFeature? _httpContextFeature, ITlsConnectionFeature? tlsConnectionFeature)
    {
        return _httpContextFeature != null && _httpContextFeature.HttpContext != null
            ? _httpContextFeature.HttpContext.Request.IsHttps
            : tlsConnectionFeature != null;
    }

    private static X509Certificate2? GetClientCertificate(IHttpContextFeature? _httpContextFeature, ITlsConnectionFeature? tlsConnectionFeature)
    {
        return _httpContextFeature != null && _httpContextFeature.HttpContext != null
            ? _httpContextFeature.HttpContext.Connection.ClientCertificate
            : tlsConnectionFeature?.ClientCertificate;
    }


    public async Task DisconnectAsync()
    {
        await _input.CompleteAsync();
        await _output.CompleteAsync();
    }

    public virtual void Dispose()
    {
        _writerLock.Dispose();
    }

    public async Task<MqttPacket?> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        try
        {
            _packetInspector?.BeginReceivePacket();

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

                            if (_packetInspector != null)
                            {
                                await _packetInspector.EndReceivePacket().ConfigureAwait(false);
                            }
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
            _input.Complete(exception);
            _output.Complete(exception);

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
                if (_packetInspector != null)
                {
                    await _packetInspector.BeginSendPacket(buffer).ConfigureAwait(false);
                }

                if (buffer.Payload.Length == 0)
                {
                    // zero copy
                    // https://github.com/dotnet/runtime/blob/e31ddfdc4f574b26231233dc10c9a9c402f40590/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs#L279
                    await _output.WriteAsync(buffer.Packet, cancellationToken).ConfigureAwait(false);
                }
                else if (_serverModeWebSocket) // server channel, and client is MQTT over WebSocket
                {
                    // Make sure the MQTT packet is in a WebSocket frame to be compatible with JavaScript WebSocket
                    WritePacketBuffer(_output, buffer);
                    await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _output.WriteAsync(buffer.Packet, cancellationToken).ConfigureAwait(false);
                    foreach (var block in buffer.Payload)
                    {
                        await _output.WriteAsync(block, cancellationToken).ConfigureAwait(false);
                    }
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
        var offset = buffer.Packet.Count;
        buffer.Payload.CopyTo(destination: span.Slice(offset));
        output.Advance(buffer.Length);
    }
}