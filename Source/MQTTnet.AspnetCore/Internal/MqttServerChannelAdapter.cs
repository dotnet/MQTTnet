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

sealed class MqttServerChannelAdapter : MqttChannel, IMqttChannelAdapter, IAspNetCoreMqttChannel
{
    public MqttServerChannelAdapter(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection, HttpContext? httpContext)
        : base(packetFormatterAdapter, connection, httpContext, packetInspector: null)
    {
        var packetFragmentationFeature = GetFeature<PacketFragmentationFeature>();
        if (packetFragmentationFeature == null)
        {
            var value = PacketFragmentationFeature.CanAllowPacketFragmentation(this, null);
            SetAllowPacketFragmentation(value);
        }
        else
        {
            var value = packetFragmentationFeature.AllowPacketFragmentationSelector(this);
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