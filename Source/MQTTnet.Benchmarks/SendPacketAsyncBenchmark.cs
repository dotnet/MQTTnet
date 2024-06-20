using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Formatter;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class SendPacketAsyncBenchmark : BaseBenchmark
    {
        MemoryStream stream;
        MqttPacketBuffer buffer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            stream = new MemoryStream(1024);
            var packet = new ArraySegment<byte>(new byte[10]);
            buffer = new MqttPacketBuffer(packet);
        }

        [Benchmark(Baseline = true)]
        public async ValueTask Before()
        {
            stream.Position = 0;
            var output = PipeWriter.Create(stream);

            WritePacketBuffer(output, buffer);
            await output.FlushAsync();
        }

        [Benchmark]
        public async ValueTask After()
        {
            stream.Position = 0;
            var output = PipeWriter.Create(stream);

            if (buffer.Payload.Length == 0)
            {
                foreach (var buffer in buffer.Packet)
                {
                    await output.WriteAsync(buffer).ConfigureAwait(false);
                }
            }
            else
            {
                WritePacketBuffer(output, buffer);
                await output.FlushAsync().ConfigureAwait(false);
            }
        }


        static void WritePacketBuffer(PipeWriter output, MqttPacketBuffer buffer)
        {
            // copy MqttPacketBuffer's Packet and Payload to the same buffer block of PipeWriter
            // MqttPacket will be transmitted within the bounds of a WebSocket frame after PipeWriter.FlushAsync

            var span = output.GetSpan(buffer.Length);

            int offset = 0;
            foreach (var segment in buffer.Packet)
            {
                segment.Span.CopyTo(span.Slice(offset));
                offset += segment.Length;
            }

            foreach (var segment in buffer.Payload)
            {
                segment.Span.CopyTo(span.Slice(offset));
                offset += segment.Length;
            }

            output.Advance(buffer.Length);
        }
    }
}