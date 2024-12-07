// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class MqttServerChannelAdapter : MqttChannel, IMqttChannelAdapter
{
    public MqttServerChannelAdapter(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection, HttpContext? httpContext)
        : base(packetFormatterAdapter, connection, httpContext, packetInspector: null)
    {
        SetAllowPacketFragmentation(connection, httpContext);
    }

    private void SetAllowPacketFragmentation(ConnectionContext connection, HttpContext? httpContext)
    {
        // When connection is from MapMqtt(),
        // the PacketFragmentationFeature instance is copied from kestrel's ConnectionContext.Features to HttpContext.Features,
        // but no longer from HttpContext.Features to connection.Features.     
        var feature = httpContext == null
            ? connection.Features.Get<PacketFragmentationFeature>()
            : httpContext.Features.Get<PacketFragmentationFeature>();

        if (feature == null)
        {
            var value = !IsWebSocketConnection;
            SetAllowPacketFragmentation(value);
        }
        else
        {
            var value = feature.AllowPacketFragmentationSelector(this);
            SetAllowPacketFragmentation(value);
        }
    }

    /// <summary>
    /// This method will never be called
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        return base.DisconnectAsync();
    }
}