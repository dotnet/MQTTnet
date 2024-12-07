// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class MqttClientChannelAdapter : IMqttChannelAdapter, IAsyncDisposable
{
    private bool _disposed = false;
    private ConnectionContext? _connection;
    private MqttChannel? _channel;
    private readonly MqttPacketFormatterAdapter _packetFormatterAdapter;
    private readonly IMqttClientChannelOptions _channelOptions;
    private readonly bool _allowPacketFragmentation;
    private readonly MqttPacketInspector? _packetInspector;

    public MqttClientChannelAdapter(
        MqttPacketFormatterAdapter packetFormatterAdapter,
        IMqttClientChannelOptions channelOptions,
        bool allowPacketFragmentation,
        MqttPacketInspector? packetInspector)
    {
        _packetFormatterAdapter = packetFormatterAdapter;
        _channelOptions = channelOptions;
        _allowPacketFragmentation = allowPacketFragmentation;
        _packetInspector = packetInspector;
    }

    public MqttPacketFormatterAdapter PacketFormatterAdapter => GetChannel().PacketFormatterAdapter;

    public long BytesReceived => GetChannel().BytesReceived;

    public long BytesSent => GetChannel().BytesSent;

    public X509Certificate2? ClientCertificate => GetChannel().ClientCertificate;

    public EndPoint? RemoteEndPoint => GetChannel().RemoteEndPoint;

    public bool IsSecureConnection => GetChannel().IsSecureConnection;

    public bool IsWebSocketConnection => GetChannel().IsSecureConnection;


    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _connection = _channelOptions switch
            {
                MqttClientTcpOptions tcpOptions => await ClientConnectionContext.CreateAsync(tcpOptions, cancellationToken).ConfigureAwait(false),
                MqttClientWebSocketOptions webSocketOptions => await ClientConnectionContext.CreateAsync(webSocketOptions, cancellationToken).ConfigureAwait(false),
                _ => throw new NotSupportedException(),
            };
            _channel = new MqttChannel(_packetFormatterAdapter, _connection, httpContext: null, _packetInspector);
            _channel.SetAllowPacketFragmentation(_allowPacketFragmentation);
        }
        catch (Exception ex)
        {
            if (!MqttChannel.WrapAndThrowException(ex))
            {
                throw;
            }
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        return GetChannel().DisconnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_channel != null)
        {
            await _channel.DisconnectAsync().ConfigureAwait(false);
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task<MqttPacket?> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        return GetChannel().ReceivePacketAsync(cancellationToken);
    }

    public void ResetStatistics()
    {
        GetChannel().ResetStatistics();
    }

    public Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        return GetChannel().SendPacketAsync(packet, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MqttChannel GetChannel()
    {
        return _channel ?? throw new InvalidOperationException();
    }
}