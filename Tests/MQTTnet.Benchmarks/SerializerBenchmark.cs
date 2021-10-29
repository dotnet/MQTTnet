using BenchmarkDotNet.Attributes;
using MQTTnet.Packets;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Channel;
using MQTTnet.Formatter;
using MQTTnet.Formatter.V3;
using BenchmarkDotNet.Jobs;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net461)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class SerializerBenchmark
    {
        MqttBasePacket _packet;
        ArraySegment<byte> _serializedPacket;
        IMqttPacketFormatter _serializer;

        [GlobalSetup]
        public void Setup()
        {
            _packet = new MqttPublishPacket
            {
                Topic = "A"
            };

            _serializer = new MqttV311PacketFormatter(new MqttPacketWriter());
            _serializedPacket = _serializer.Encode(_packet).ToArray();
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
            var reader = new MqttChannelAdapter(channel, new MqttPacketFormatterAdapter(new MqttPacketWriter()), null, new MqttNetEventLogger());

            for (var i = 0; i < 10000; i++)
            {
                channel.Reset();

                var header = reader.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        class BenchmarkMqttChannel : IMqttChannel
        {
            readonly ArraySegment<byte> _buffer;
            int _position;

            public BenchmarkMqttChannel(ArraySegment<byte> buffer)
            {
                _buffer = buffer;
                _position = _buffer.Offset;
            }

            public string Endpoint { get; } = string.Empty;

            public bool IsSecureConnection { get; } = false;

            public X509Certificate2 ClientCertificate { get; }

            public void Reset()
            {
                _position = _buffer.Offset;
            }

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task DisconnectAsync(CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                Array.Copy(_buffer.Array, _position, buffer, offset, count);
                _position += count;

                return Task.FromResult(count);
            }

            public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
