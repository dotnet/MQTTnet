﻿using BenchmarkDotNet.Attributes;
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
                using (var headerStream = new MemoryStream(Join(_serializedPacket)))
                {
                    var channel = new TestMqttChannel(headerStream);

                    var header = MqttPacketReader.ReadFixedHeaderAsync(channel, CancellationToken.None).GetAwaiter().GetResult();
                    
                    using (var bodyStream = new MemoryStream(Join(_serializedPacket), (int)headerStream.Position, header.RemainingLength))
                    {
                        _serializer.Deserialize(new ReceivedMqttPacket(header.Flags, new MqttPacketBodyReader(bodyStream.ToArray())));
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
