// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore
{
    public static class ReaderExtensions
    {
        public static bool TryDecode(this MqttPacketFormatterAdapter formatter, 
            SpanBasedMqttPacketBodyReader reader, 
            in ReadOnlySequence<byte> input, 
            out MqttBasePacket packet, 
            out SequencePosition consumed, 
            out SequencePosition observed,
            out int bytesRead)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            packet = null;
            consumed = input.Start;
            observed = input.End;
            bytesRead = 0;
            var copy = input;

            if (copy.Length < 2)
            {
                return false;
            }

            var fixedheader = copy.First.Span[0];
            if (!TryReadBodyLength(ref copy, out int headerLength, out var bodyLength))
            {
                return false;
            }

            if (copy.Length < bodyLength)
            {
                return false;
            }

            var bodySlice = copy.Slice(0, bodyLength);
            var buffer = bodySlice.GetMemory();
            reader.SetBuffer(buffer);

            var receivedMqttPacket = new ReceivedMqttPacket(fixedheader, reader, buffer.Length + 2);

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

        private static ReadOnlyMemory<byte> GetMemory(this in ReadOnlySequence<byte> input)
        {
            if (input.IsSingleSegment)
            {
                return input.First;
            }

            // Should be rare
            return input.ToArray();
        }

        private static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int headerLength, out int bodyLength)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            var index = 1;
            headerLength = 0;
            bodyLength = 0;
            
            var temp = input.Slice(0, Math.Min(5, input.Length)).GetMemory().Span;

            do
            {
                if (index == temp.Length)
                {
                    return false;
                }

                encodedByte = temp[index];
                index++;

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException($"Remaining length is invalid (Data={string.Join(",", temp.Slice(1, index).ToArray())}).");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            input = input.Slice(index);

            headerLength = index;
            bodyLength = value;
            return true;
        }
    }
}
