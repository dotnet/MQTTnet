using System;
using System.Buffers;
using System.IO;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MQTTnet.AspNetCore
{
    public static class ReaderExtensions 
    {
        public static MqttPacketHeader ReadHeader(this ref ReadOnlySequence<byte> input)
        {
            if (input.Length < 2)
            {
                return null;
            }

            var fixedHeader = input.First.Span[0];
            var controlPacketType = (MqttControlPacketType)(fixedHeader >> 4);
            var bodyLength = ReadBodyLength(ref input);

            return new MqttPacketHeader
            {
                FixedHeader = fixedHeader,
                ControlPacketType = controlPacketType,
                BodyLength = bodyLength
            };
        }

        private static int ReadBodyLength(ref ReadOnlySequence<byte> input)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            var index = 1;

            var temp = input.Slice(0, Math.Min(5, input.Length)).GetArray();

            do
            {
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

            return value;
        }



        public static byte[] GetArray(this in ReadOnlySequence<byte> input)
        {
            if (input.IsSingleSegment)
            {
                return input.First.Span.ToArray();
            }

            // Should be rare
            return input.ToArray();
        }

        public static bool TryDeserialize(this IMqttPacketSerializer serializer, ref ReadOnlySequence<byte> input, out MqttBasePacket packet)
        {
            packet = null;
            var copy = input;
            var header = copy.ReadHeader();
            if (header == null || copy.Length < header.BodyLength)
            {
                return false;
            }

            input = copy.Slice(header.BodyLength);
            var bodySlice = copy.Slice(0, header.BodyLength);
            using (var body = new MemoryStream(bodySlice.GetArray()))
            {
                packet = serializer.Deserialize(header, body);
                return true;
            }
        }

        public static bool TryDeserialize(this IMqttPacketSerializer serializer, in ReadOnlySequence<byte> input, out MqttBasePacket packet, out SequencePosition consumed, out SequencePosition observed)
        {
            packet = null;
            var copy = input;
            var header = copy.ReadHeader();
            if (header == null || copy.Length < header.BodyLength)
            {
                consumed = input.Start;
                observed = input.End;
                return false;
            }

            var bodySlice = copy.Slice(0, header.BodyLength);
            using (var body = new MemoryStream(bodySlice.GetArray()))
            {
                packet = serializer.Deserialize(header, body);
                consumed = bodySlice.End;
                observed = bodySlice.End;
                return true;
            }
        }
    }
}
