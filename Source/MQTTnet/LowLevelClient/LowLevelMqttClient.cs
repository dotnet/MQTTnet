// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.LowLevelClient;

public sealed class LowLevelMqttClient : ILowLevelMqttClient
{
    readonly IMqttClientAdapterFactory _clientAdapterFactory;
    readonly AsyncEvent<InspectMqttPacketEventArgs> _inspectPacketEvent = new();
    readonly MqttNetSourceLogger _logger;
    readonly IMqttNetLogger _rootLogger;

    IMqttChannelAdapter _adapter;

    public LowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory, IMqttNetLogger logger)
    {
        _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));

        _rootLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = logger.WithSource(nameof(LowLevelMqttClient));
    }

    public event Func<InspectMqttPacketEventArgs, Task> InspectPacketAsync
    {
        add => _inspectPacketEvent.AddHandler(value);
        remove => _inspectPacketEvent.RemoveHandler(value);
    }

    public bool IsConnected => _adapter != null;

    public async Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_adapter != null)
        {
            throw new InvalidOperationException("Low level MQTT client is already connected. Disconnect first before connecting again.");
        }

        MqttPacketInspector packetInspector = null;
        if (_inspectPacketEvent.HasHandlers)
        {
            packetInspector = new MqttPacketInspector(_inspectPacketEvent, _rootLogger);
        }

        var newAdapter = _clientAdapterFactory.CreateClientAdapter(options, packetInspector, _rootLogger);

        try
        {
            _logger.Verbose("Trying to connect with server '{0}'", options.ChannelOptions);
            await newAdapter.ConnectAsync(cancellationToken).ConfigureAwait(false);
            _logger.Verbose("Connection with server established");
        }
        catch
        {
            _adapter?.Dispose();
            throw;
        }

        _adapter = newAdapter;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var adapter = _adapter;
        if (adapter == null)
        {
            throw new InvalidOperationException("Low level MQTT client is not connected.");
        }

        try
        {
            await adapter.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _adapter?.Dispose();
        _adapter = null;
    }

    public async Task<MqttPacket> ReceiveAsync(CancellationToken cancellationToken)
    {
        var adapter = _adapter;
        if (adapter == null)
        {
            throw new InvalidOperationException("Low level MQTT client is not connected.");
        }

        try
        {
            var receivedPacket = await adapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
            if (receivedPacket == null)
            {
                // Graceful socket close.
                throw new MqttCommunicationException("The connection is closed.");
            }

            return receivedPacket;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(packet);

        var adapter = _adapter;
        if (adapter == null)
        {
            throw new InvalidOperationException("Low level MQTT client is not connected.");
        }

        try
        {
            await adapter.SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Dispose();
            throw;
        }
    }
}