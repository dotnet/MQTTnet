// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Adapter;

public sealed class MqttPacketInspector
{
    readonly AsyncEvent<InspectMqttPacketEventArgs> _asyncEvent;
    readonly MqttNetSourceLogger _logger;

    readonly Pipe _pipeIn = new();
    readonly Pipe _pipeOut = new();
    ReceiveState _receiveState = ReceiveState.Disable;

    public MqttPacketInspector(AsyncEvent<InspectMqttPacketEventArgs> asyncEvent, IMqttNetLogger logger)
    {
        _asyncEvent = asyncEvent ?? throw new ArgumentNullException(nameof(asyncEvent));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(MqttPacketInspector));
    }

    public async Task BeginSendPacket(MqttPacketBuffer buffer)
    {
        if (!_asyncEvent.HasHandlers)
        {
            return;
        }

        // Create a copy of the actual packet so that the inspector gets no access
        // to the internal buffers. This is waste of memory but this feature is only
        // intended for debugging etc. so that this is OK.
        var writer = _pipeOut.Writer;
        await writer.WriteAsync(buffer.Packet).ConfigureAwait(false);
        foreach (var memory in buffer.Payload)
        {
            await writer.WriteAsync(memory).ConfigureAwait(false);
        }

        await writer.CompleteAsync().ConfigureAwait(false);
        await InspectPacketAsync(_pipeOut.Reader, MqttPacketFlowDirection.Outbound).ConfigureAwait(false);

        // reset pipe
        await _pipeOut.Reader.CompleteAsync().ConfigureAwait(false);
        _pipeOut.Reset();
    }

    public void BeginReceivePacket()
    {
        if (_asyncEvent.HasHandlers)
        {
            // This shouldn't happen, but we need to be able to accommodate the unexpected.
            if (_receiveState == ReceiveState.Fill)
            {
                _pipeIn.Writer.Complete();
                _pipeIn.Reader.Complete();
                _pipeIn.Reset();

                _logger.Warning("An EndReceivePacket() operation was unexpectedly lost.");
            }

            _receiveState = ReceiveState.Begin;
        }
        else
        {
            _receiveState = ReceiveState.Disable;
        }
    }

    public void FillReceiveBuffer(ReadOnlySpan<byte> buffer)
    {
        if (_receiveState == ReceiveState.Disable)
        {
            return;
        }

        if (_receiveState == ReceiveState.End)
        {
            throw new InvalidOperationException("FillReceiveBuffer is not allowed in End state.");
        }

        _pipeIn.Writer.Write(buffer);
        _receiveState = ReceiveState.Fill;
    }

    public void FillReceiveBuffer(ReadOnlySequence<byte> buffer)
    {
        if (_receiveState == ReceiveState.Disable)
        {
            return;
        }

        if (_receiveState == ReceiveState.End)
        {
            throw new InvalidOperationException("FillReceiveBuffer is not allowed in End state.");
        }

        var writer = _pipeIn.Writer;
        foreach (var memory in buffer)
        {
            writer.Write(memory.Span);
        }

        _receiveState = ReceiveState.Fill;
    }


    public async Task EndReceivePacket()
    {
        if (_receiveState == ReceiveState.Disable || _receiveState == ReceiveState.End)
        {
            return;
        }

        await _pipeIn.Writer.FlushAsync().ConfigureAwait(false);
        await _pipeIn.Writer.CompleteAsync().ConfigureAwait(false);
        await InspectPacketAsync(_pipeIn.Reader, MqttPacketFlowDirection.Inbound).ConfigureAwait(false);

        // reset pipe
        await _pipeIn.Reader.CompleteAsync().ConfigureAwait(false);
        _pipeIn.Reset();

        _receiveState = ReceiveState.End;
    }


    async Task InspectPacketAsync(PipeReader pipeReader, MqttPacketFlowDirection direction)
    {
        try
        {
            var buffer = await ReadBufferAsync(pipeReader, default).ConfigureAwait(false);
            var eventArgs = new InspectMqttPacketEventArgs(direction, buffer);
            await _asyncEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error while inspecting packet.");
        }
    }

    static async ValueTask<ReadOnlySequence<byte>> ReadBufferAsync(PipeReader pipeReader, CancellationToken cancellationToken)
    {
        var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        while (!readResult.IsCompleted)
        {
            pipeReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.Start);
            readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        return readResult.Buffer;
    }

    private enum ReceiveState
    {
        Disable,
        Begin,
        Fill,
        End,
    }
}