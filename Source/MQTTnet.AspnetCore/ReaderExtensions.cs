// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore;

public static class ReaderExtensions
{
    public static bool TryDecode(
        this MqttPacketFormatterAdapter formatter,
        in ReadOnlySequence<byte> input,
        out MqttPacket packet,
        out SequencePosition consumed,
        out SequencePosition observed,
        out int bytesRead)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        packet = null;
        consumed = input.Start;
        observed = input.End;
        bytesRead = 0;
        var copy = input;

        if (copy.Length < 2)
        {
            return false;
        }

        if (!TryReadBodyLength(ref copy, out var headerLength, out var bodyLength))
        {
            return false;
        }

        var fixedHeader = copy.First.Span[0];
        copy = copy.Slice(headerLength);
        if (copy.Length < bodyLength)
        {
            return false;
        }

        var bodySlice = copy.Slice(0, bodyLength);
        var bodySegment = GetArraySegment(ref bodySlice);

        var receivedMqttPacket = new ReceivedMqttPacket(fixedHeader, bodySegment, headerLength + bodyLength);
        if (formatter.ProtocolVersion == MqttProtocolVersion.Unknown)
        {
            formatter.DetectProtocolVersion(receivedMqttPacket);
        }

        packet = formatter.Decode(receivedMqttPacket);
        consumed = bodySlice.End;
        observed = bodySlice.End;
        bytesRead = headerLength + bodyLength;
        return true;
    }

    static ArraySegment<byte> GetArraySegment(ref ReadOnlySequence<byte> input)
    {
        if (input.IsSingleSegment && MemoryMarshal.TryGetArray(input.First, out var segment))
        {
            return segment;
        }

        // Should be rare
        var array = input.ToArray();
        return new ArraySegment<byte>(array);
    }


    static void ThrowProtocolViolationException(ReadOnlySpan<byte> valueSpan, int index)
    {
        throw new MqttProtocolViolationException($"Remaining length is invalid (Data={string.Join(",", valueSpan.Slice(1, index).ToArray())}).");
    }

    static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int headerLength, out int bodyLength)
    {
        var valueSequence = input.Slice(0, Math.Min(5, input.Length));
        if (valueSequence.IsSingleSegment)
        {
            var valueSpan = valueSequence.FirstSpan;

            return TryReadBodyLength(valueSpan, out headerLength, out bodyLength);
        }
        else
        {
            Span<byte> valueSpan = stackalloc byte[8];
            valueSequence.CopyTo(valueSpan);
            valueSpan = valueSpan.Slice(0, (int)valueSequence.Length);
            return TryReadBodyLength(valueSpan, out headerLength, out bodyLength);
        }
    }

    static bool TryReadBodyLength(ReadOnlySpan<byte> span, out int headerLength, out int bodyLength)
    {
        // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
        var multiplier = 1;
        var value = 0;
        byte encodedByte;
        var index = 1;
        headerLength = 0;
        bodyLength = 0;

        do
        {
            if (index == span.Length)
            {
                return false;
            }

            encodedByte = span[index];
            index++;

            value += (byte)(encodedByte & 127) * multiplier;
            if (multiplier > 128 * 128 * 128)
            {
                ThrowProtocolViolationException(span, index);
            }

            multiplier *= 128;
        } while ((encodedByte & 128) != 0);

        headerLength = index;
        bodyLength = value;
        return true;
    }
}