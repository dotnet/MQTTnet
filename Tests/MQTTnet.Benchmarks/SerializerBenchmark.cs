using BenchmarkDotNet.Attributes;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using MQTTnet.Internal;
using MQTTnet.Server;
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
        private MqttApplicationMessage _message;
        private MqttBasePacket _packet;
        private ArraySegment<byte> _serializedPacket;
        private MqttPacketSerializer _serializer;

        [GlobalSetup]
        public void Setup()
        {
            _message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();

            _packet = _message.ToPublishPacket();
            _serializer = new MqttPacketSerializer();
            _serializedPacket = _serializer.Serialize(_packet);
        }

        [Benchmark]
        public void Serialize_10000_Messages()
        {
            for (var i = 0; i < 10000; i++)
            {
                _serializer.Serialize(_packet);
            }
        }

        [Benchmark]
        public void Deserialize_10000_Messages()
        {
            for (var i = 0; i < 10000; i++)
            {
                using (var headerStream = new MemoryStream(Join(_serializedPacket)))
                {
                    var header = MqttPacketReader.ReadHeaderAsync(new TestMqttChannel(headerStream), CancellationToken.None).GetAwaiter().GetResult();

                    using (var bodyStream = new MemoryStream(Join(_serializedPacket), (int)headerStream.Position, header.BodyLength))
                    {
                        var deserializedPacket = _serializer.Deserialize(header, bodyStream);
                    }
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
