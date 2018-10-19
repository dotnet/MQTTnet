using BenchmarkDotNet.Attributes;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using MQTTnet.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Channel;

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
                _serializer.FreeBuffer();
            }
        }

        [Benchmark]
        public void Deserialize_10000_Messages()
        {
            var channel = new BenchmarkMqttChannel(_serializedPacket);
            var fixedHeader = new byte[2];
            var singleByteBuffer = new byte[1];

            for (var i = 0; i < 10000; i++)
            {
                channel.Reset();

                var header = MqttPacketReader.ReadFixedHeaderAsync(channel, fixedHeader, singleByteBuffer, CancellationToken.None).GetAwaiter().GetResult();

                var receivedPacket = new ReceivedMqttPacket(
                    header.Flags,
                    new MqttPacketBodyReader(_serializedPacket.Array, _serializedPacket.Count - header.RemainingLength, _serializedPacket.Array.Length));

                _serializer.Deserialize(receivedPacket);
            }
        }

        private class BenchmarkMqttChannel : IMqttChannel
        {
            private readonly ArraySegment<byte> _buffer;
            private int _position;

            public BenchmarkMqttChannel(ArraySegment<byte> buffer)
            {
                _buffer = buffer;
                _position = _buffer.Offset;
            }

            public string Endpoint { get; }

            public void Reset()
            {
                _position = _buffer.Offset;
            }

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task DisconnectAsync()
            {
                throw new NotImplementedException();
            }

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                Array.Copy(_buffer.Array, _position, buffer, offset, count);
                _position += count;

                return Task.FromResult(count);
            }

            public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
