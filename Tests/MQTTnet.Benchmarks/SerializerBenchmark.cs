using BenchmarkDotNet.Attributes;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using MQTTnet.Internal;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Attributes.Exporters;
using System;
using System.Threading;
using System.IO;
using MQTTnet.Core.Internal;

namespace MQTTnet.Benchmarks
{
    [ClrJob]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class SerializerBenchmark
    {
        private MqttBasePacket _packet;
        private byte[] _serializedPacket;
        private MqttPacketSerializer _serializer;

        [Params(1, 10000)]
        public int Iterations { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();

            _packet = message.ToPublishPacket();
            _serializer = new MqttPacketSerializer();
            _serializedPacket = Join(_serializer.Serialize(_packet));
        }

        [Benchmark]
        public void Serialize_10000_Messages()
        {
            for (var i = 0; i < Iterations; i++)
            {
                _serializer.Serialize(_packet);
            }
        }

        [Benchmark]
        public void Deserialize_10000_Messages()
        {
            for (var i = 0; i < Iterations; i++)
            {
                using (var headerStream = new MemoryStream(_serializedPacket))
                {
                    var header = MqttPacketReader.ReadHeaderAsync(new TestMqttChannel(headerStream), CancellationToken.None).GetAwaiter().GetResult();
                    _serializer.Deserialize(header, _serializedPacket.AsSpan((int)headerStream.Position, header.BodyLength));
                }
            }
        }
        
        private static byte[] Join(params ArraySegment<byte>[] chunks)
        {
            var buffer = new MemoryStream();
            foreach (var chunk in chunks)
            {
                buffer.Write(chunk.Array, chunk.Offset, chunk.Count);
            }

            return buffer.ToArray();
        }
    }
}
