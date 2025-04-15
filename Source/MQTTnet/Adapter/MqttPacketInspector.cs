// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using MQTTnet.Internal;

namespace MQTTnet.Adapter;

public sealed class MqttPacketInspector
{
    readonly AsyncEvent<InspectMqttPacketEventArgs> _asyncEvent;
    readonly MqttNetSourceLogger _logger;

    MemoryStream _receivedPacketBuffer;

    public MqttPacketInspector(AsyncEvent<InspectMqttPacketEventArgs> asyncEvent, IMqttNetLogger logger)
    {
        _asyncEvent = asyncEvent ?? throw new ArgumentNullException(nameof(asyncEvent));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(MqttPacketInspector));
    }

    public void BeginReceivePacket()
    {
        if (!_asyncEvent.HasHandlers)
        {
            return;
        }

        _receivedPacketBuffer ??= new MemoryStream();
        _receivedPacketBuffer?.SetLength(0);
    }

    public Task BeginSendPacket(MqttPacketBuffer buffer)
    {
        if (!_asyncEvent.HasHandlers)
        {
            return CompletedTask.Instance;
        }

        // Create a copy of the actual packet so that the inspector gets no access
        // to the internal buffers. This is waste of memory but this feature is only
        // intended for debugging etc. so that this is OK.
        var bufferCopy = buffer.ToArray();

        return InspectPacket(bufferCopy, MqttPacketFlowDirection.Outbound);
    }

    public Task EndReceivePacket()
    {
        if (!_asyncEvent.HasHandlers)
        {
            return CompletedTask.Instance;
        }

        var buffer = _receivedPacketBuffer.ToArray();
        _receivedPacketBuffer.SetLength(0);

        return InspectPacket(buffer, MqttPacketFlowDirection.Inbound);
    }

    public void FillReceiveBuffer(byte[] buffer)
    {
        if (!_asyncEvent.HasHandlers)
        {
            return;
        }

        _receivedPacketBuffer?.Write(buffer, 0, buffer.Length);
    }

    async Task InspectPacket(byte[] buffer, MqttPacketFlowDirection direction)
    {
        try
        {
            var eventArgs = new InspectMqttPacketEventArgs(direction, buffer);
            await _asyncEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error while inspecting packet.");
        }
    }
}