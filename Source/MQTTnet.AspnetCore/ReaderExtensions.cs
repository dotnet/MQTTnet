using System;
using System.Buffers;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Serializer;

namespace MQTTnet.AspNetCore
{
    public static class ReaderExtensions
    {
        public static bool TryDeserialize(this IMqttPacketSerializer serializer, in ReadOnlySequence<byte> input, out MqttBasePacket packet, out SequencePosition consumed, out SequencePosition observed)
        {
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
            packet = serializer.Deserialize(new ReceivedMqttPacket(fixedheader, new MqttPacketBodyReader(bodySlice.GetArray(), 0)));
            consumed = bodySlice.End;
            observed = bodySlice.End;
            return true;
        }

        private static byte[] GetArray(this in ReadOnlySequence<byte> input)
        {
            if (input.IsSingleSegment)
            {
                return input.First.Span.ToArray();
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

            var temp = input.Slice(0, Math.Min(5, input.Length)).GetArray();

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
                    throw new MqttProtocolViolationException($"Remaining length is invalid (Data={string.Join(",", temp.AsSpan(1, index).ToArray())}).");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            input = input.Slice(index);

            result = value;
            return true;
        }
    }
}
