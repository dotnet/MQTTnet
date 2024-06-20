// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MQTTnet.Buffers;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Internal;

namespace MQTTnet.Adapter;

public sealed class MqttPacketInspector
{
    readonly AsyncEvent<InspectMqttPacketEventArgs> _asyncEvent;
    readonly MqttNetSourceLogger _logger;

    ArrayPoolMemoryStream _receivedPacketBuffer;

    public MqttPacketInspector(AsyncEvent<InspectMqttPacketEventArgs> asyncEvent, IMqttNetLogger logger)
    {
        _asyncEvent = asyncEvent ?? throw new ArgumentNullException(nameof(asyncEvent));

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _logger = logger.WithSource(nameof(MqttPacketInspector));
    }

    public void BeginReceivePacket()
    {
        if (!_asyncEvent.HasHandlers)
        {
            return;
        }

        if (_receivedPacketBuffer == null)
        {
            _receivedPacketBuffer = new ArrayPoolMemoryStream(1024);
        }
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
        var bufferCopy = new ReadOnlySequence<byte>(buffer.ToArray());

        return InspectPacket(bufferCopy, null, MqttPacketFlowDirection.Outbound);
    }

    public Task EndReceivePacket()
    {
        if (!_asyncEvent.HasHandlers)
        {
            return CompletedTask.Instance;
        }

        var sequence = _receivedPacketBuffer.GetReadOnlySequence();

        // set sequence and transform ownership of stream
        Task t = InspectPacket(sequence, _receivedPacketBuffer, MqttPacketFlowDirection.Inbound);
        _receivedPacketBuffer = null;

        return t;
    }

    public void FillReceiveBuffer(ReadOnlySpan<byte> buffer)
    {
        if (!_asyncEvent.HasHandlers)
        {
            return;
        }

        _receivedPacketBuffer?.Write(buffer);
    }

    async Task InspectPacket(ReadOnlySequence<byte> sequence, IDisposable owner, MqttPacketFlowDirection direction)
    {
        try
        {
            var eventArgs = new InspectMqttPacketEventArgs(direction, sequence);
            await _asyncEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            owner?.Dispose();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error while inspecting packet.");
        }
    }
}