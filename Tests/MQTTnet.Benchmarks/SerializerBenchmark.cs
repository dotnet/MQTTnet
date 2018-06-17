using BenchmarkDotNet.Attributes;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using MQTTnet.Internal;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Attributes.Exporters;
using System;
using System.Threading;
using System.IO;
using MQTTnet.Adapter;

namespace MQTTnet.Benchmarks
{
    [ClrJob]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class SerializerBenchmark
    {
        private MqttBasePacket _packet;
        private ArraySegment<byte> _serializedPacket;
        private MqttPacketSerializer _serializer;

        [GlobalSetup]
        public void Setup()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();

            _packet = message.ToPublishPacket();
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
                using (var stream = new MemoryStream())
                {
                    stream.Write(_serializedPacket.Array, _serializedPacket.Offset, _serializedPacket.Count);
                    stream.Position = 0;

                    var channel = new TestMqttChannel(stream);

                    var header = MqttPacketReader.ReadFixedHeaderAsync(channel, CancellationToken.None).GetAwaiter().GetResult();
                    _serializer.Deserialize(new ReceivedMqttPacket(header.Flags, new MqttPacketBodyReader(_serializedPacket.Array, _serializedPacket.Count - header.RemainingLength)));
                }
            }
        }
    }
}
