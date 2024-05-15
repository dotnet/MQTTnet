using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class ReaderExtensionsBenchmark
    {
        MqttPacketFormatterAdapter mqttPacketFormatter;
        MemoryStream stream;

        [GlobalSetup]
        public void GlobalSetup()
        {
            mqttPacketFormatter = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
            var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic("topic")
            .WithPayload(new byte[10 * 1024])
            .Build();

            var packet = MqttPublishPacketFactory.Create(mqttMessage);

            var buffer = mqttPacketFormatter.Encode(packet);
            stream = new MemoryStream();
            stream.Write(buffer.Packet);
            stream.Write(buffer.Payload);
            mqttPacketFormatter.Cleanup();
        }

        [Benchmark(Baseline = true)]
        public async Task Before()
        {
            stream.Position = 0;
            var input = PipeReader.Create(stream);

            while (true)
            {
                ReadResult readResult;
                var readTask = input.ReadAsync();
                if (readTask.IsCompleted)
                {
                    readResult = readTask.Result;
                }
                else
                {
                    readResult = await readTask.ConfigureAwait(false);
                }

                var buffer = readResult.Buffer;

                var consumed = buffer.Start;
                var observed = buffer.Start;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        if (ReaderExtensions_Before.TryDecode(mqttPacketFormatter, buffer, out var packet, out consumed, out observed, out var received))
                        {
                            break;
                        }
                    }
                    else if (readResult.IsCompleted)
                    {
                        throw new MqttCommunicationException("Connection Aborted");
                    }
                }
                finally
                {
                    // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                    // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                    // before yielding the read again.
                    input.AdvanceTo(consumed, observed);
                }
            }
        }

        [Benchmark]
        public async Task After()
        {
            stream.Position = 0;
            var input = PipeReader.Create(stream);

            while (true)
            {
                ReadResult readResult;
                var readTask = input.ReadAsync();
                if (readTask.IsCompleted)
                {
                    readResult = readTask.Result;
                }
                else
                {
                    readResult = await readTask.ConfigureAwait(false);
                }

                var buffer = readResult.Buffer;

                var consumed = buffer.Start;
                var observed = buffer.Start;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        if (ReaderExtensions.TryDecode(mqttPacketFormatter, buffer, out var packet, out consumed, out observed, out var received))
                        {
                            break;
                        }
                    }
                    else if (readResult.IsCompleted)
                    {
                        throw new MqttCommunicationException("Connection Aborted");
                    }
                }
                finally
                {
                    // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                    // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                    // before yielding the read again.
                    input.AdvanceTo(consumed, observed);
                }
            }
        }

        public static class ReaderExtensions_Before
        {
            public static bool TryDecode(MqttPacketFormatterAdapter formatter,
                in ReadOnlySequence<byte> input,
                out MqttPacket packet,
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

                var fixedHeader = copy.First.Span[0];
                if (!TryReadBodyLength(ref copy, out int headerLength, out var bodyLength))
                {
                    return false;
                }

                if (copy.Length < bodyLength)
                {
                    return false;
                }

                var bodySlice = copy.Slice(0, bodyLength);
                var buffer = GetMemory(bodySlice).ToArray();

                var receivedMqttPacket = new ReceivedMqttPacket(fixedHeader, new ArraySegment<byte>(buffer, 0, buffer.Length), buffer.Length + 2);

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

            static ReadOnlyMemory<byte> GetMemory(in ReadOnlySequence<byte> input)
            {
                if (input.IsSingleSegment)
                {
                    return input.First;
                }

                // Should be rare
                return input.ToArray();
            }

            static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int headerLength, out int bodyLength)
            {
                // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
                var multiplier = 1;
                var value = 0;
                byte encodedByte;
                var index = 1;
                headerLength = 0;
                bodyLength = 0;

                var temp = GetMemory(input.Slice(0, Math.Min(5, input.Length))).Span;

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
}
