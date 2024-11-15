// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Internal;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
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

    public MqttClientChannelAdapter(MqttPacketFormatterAdapter packetFormatterAdapter, IMqttClientChannelOptions channelOptions)
    {
        _packetFormatterAdapter = packetFormatterAdapter;
        _channelOptions = channelOptions;
    }

    public MqttPacketFormatterAdapter PacketFormatterAdapter => GetChannel().PacketFormatterAdapter;

    public long BytesReceived => GetChannel().BytesReceived;

    public long BytesSent => GetChannel().BytesSent;

    public X509Certificate2? ClientCertificate => GetChannel().ClientCertificate;

    public string? Endpoint => GetChannel().Endpoint;

    public bool IsSecureConnection => GetChannel().IsSecureConnection;


    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _connection = _channelOptions switch
        {
            MqttClientTcpOptions tcpOptions => await ClientConnectionContext.CreateAsync(tcpOptions, cancellationToken),
            MqttClientWebSocketOptions webSocketOptions => await ClientConnectionContext.CreateAsync(webSocketOptions, cancellationToken),
            _ => throw new NotSupportedException(),
        };
        _channel = new MqttChannel(_packetFormatterAdapter, _connection);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await GetChannel().DisconnectAsync();
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
            await _channel.DisconnectAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
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