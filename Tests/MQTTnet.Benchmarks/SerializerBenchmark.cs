using BenchmarkDotNet.Attributes;
using MQTTnet.Packets;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Channel;
using MQTTnet.Formatter;
using MQTTnet.Formatter.V3;

namespace MQTTnet.Benchmarks
{
    [ClrJob]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class SerializerBenchmark
    {
        private MqttBasePacket _packet;
        private ArraySegment<byte> _serializedPacket;
        private IMqttPacketFormatter _serializer;

        [GlobalSetup]
        public void Setup()
        {
            _packet = new MqttPublishPacket
            {
                Topic = "A"
            };

            _serializer = new MqttV311PacketFormatter();
            _serializedPacket = _serializer.Encode(_packet);
        }

        [Benchmark]
        public void Serialize_10000_Messages()
        {
            for (var i = 0; i < 10000; i++)
            {
                _serializer.Encode(_packet);
                _serializer.FreeBuffer();
            }
        }

        [Benchmark]
        public void Deserialize_10000_Messages()
        {
            var channel = new BenchmarkMqttChannel(_serializedPacket);
            var fixedHeader = new byte[2];
            var reader = new MqttPacketReader(channel);

            for (var i = 0; i < 10000; i++)
            {
                channel.Reset();

                var header = reader.ReadFixedHeaderAsync(fixedHeader, CancellationToken.None).GetAwaiter().GetResult().FixedHeader;

                var receivedPacket = new ReceivedMqttPacket(
                    header.Flags,
                    new MqttPacketBodyReader(_serializedPacket.Array, (ulong)(_serializedPacket.Count - header.RemainingLength), (ulong)_serializedPacket.Array.Length), 0);

                _serializer.Decode(receivedPacket);
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
