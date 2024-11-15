// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class MqttServerChannelAdapter : IMqttChannelAdapter
{
    private readonly MqttChannel _channel;

    public MqttServerChannelAdapter(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection)
    {
        _channel = new MqttChannel(packetFormatterAdapter, connection);
    }

    public MqttPacketFormatterAdapter PacketFormatterAdapter => _channel.PacketFormatterAdapter;

    public long BytesReceived => _channel.BytesReceived;

    public long BytesSent => _channel.BytesSent;

    public X509Certificate2? ClientCertificate => _channel.ClientCertificate;

    public string? Endpoint => _channel.Endpoint;

    public bool IsSecureConnection => _channel.IsSecureConnection;


    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        return _channel.DisconnectAsync();
    }

    public void Dispose()
    {
        _channel.Dispose();
    }

    public Task<MqttPacket?> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        return _channel.ReceivePacketAsync(cancellationToken);
    }

    public void ResetStatistics()
    {
        _channel.ResetStatistics();
    }

    public Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        return _channel.SendPacketAsync(packet, cancellationToken);
    }
}