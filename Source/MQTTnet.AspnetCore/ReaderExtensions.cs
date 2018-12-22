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
        public static bool TryDecode(this MqttPacketFormatterAdapter formatter, SpanBasedMqttPacketBodyReader reader, ReceivedMqttPacket receivedMqttPacket, in ReadOnlySequence<byte> input, out MqttBasePacket packet, out SequencePosition consumed, out SequencePosition observed)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            packet = null;
            consumed = input.Start;
            observed = input.End;
            var copy = input;

            if (copy.Length < 2)
            {
                return false;
            }

            var fixedheader = copy.First.Span[0];
            if (!TryReadBodyLength(ref copy, out var bodyLength))
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
            receivedMqttPacket.FixedHeader = fixedheader;

            if (!formatter.ProtocolVersion.HasValue)
            {
                formatter.DetectProtocolVersion(receivedMqttPacket);
            }

            packet = formatter.Decode(receivedMqttPacket);
            consumed = bodySlice.End;
            observed = bodySlice.End;
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

        private static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int result)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            var index = 1;
            result = 0;

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

            result = value;
            return true;
        }
    }
}
