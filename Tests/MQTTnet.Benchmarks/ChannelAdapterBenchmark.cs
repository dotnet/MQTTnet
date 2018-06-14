using BenchmarkDotNet.Attributes;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using System;
using System.IO;
using System.Threading;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class ChannelAdapterBenchmark
    {
        private MqttChannelAdapter _channelAdapter;
        private int _iterations;
        private MemoryStream _stream;
        private MqttPublishPacket _packet;

        [GlobalSetup]
        public void Setup()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();

            _packet = message.ToPublishPacket();
            var serializer = new MqttPacketSerializer();
            var serializedPacket = Join(serializer.Serialize(_packet));

            _iterations = 10000;

            _stream = new MemoryStream(_iterations * serializedPacket.Length);

            for (var i = 0; i < _iterations; i++)
            {
                _stream.Write(serializedPacket, 0, serializedPacket.Length);
            }

            _stream.Position = 0;

            var channel = new TestMqttChannel(_stream);
            
            _channelAdapter = new MqttChannelAdapter(channel, serializer, new MqttNetLogger().CreateChildLogger(nameof(MqttChannelAdapter)));
        }

        [Benchmark]
        public void Receive_10000_Messages()
        {
            _stream.Position = 0;

            for (var i = 0; i < 10000; i++)
            {
                _channelAdapter.ReceivePacketAsync(TimeSpan.Zero, CancellationToken.None).GetAwaiter().GetResult();
            }

            _stream.Position = 0;
        }

        [Benchmark]
        public void Send_10000_Messages()
        {
            _stream.Position = 0;

            for (var i = 0; i < 10000; i++)
            {
                _channelAdapter.SendPacketAsync(TimeSpan.FromSeconds(15), _packet, CancellationToken.None).GetAwaiter().GetResult();
            }

            _stream.Position = 0;
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
