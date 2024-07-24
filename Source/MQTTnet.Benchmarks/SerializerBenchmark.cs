// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class SerializerBenchmark : BaseBenchmark
    {
        MqttPacket _packet;
        ArraySegment<byte> _serializedPacket;
        IMqttPacketFormatter _serializer;
        MqttBufferWriter _bufferWriter;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _packet = new MqttPublishPacket
            {
                Topic = "A"
            };

            _bufferWriter = new MqttBufferWriter(4096, 65535);
            _serializer = new MqttV3PacketFormatter(_bufferWriter, MqttProtocolVersion.V311);
            _serializedPacket = _serializer.Encode(_packet).Join();
        }

        [Benchmark]
        public void Serialize_10000_Messages()
        {
            for (var i = 0; i < 10000; i++)
            {
                _serializer.Encode(_packet);
                _bufferWriter.Cleanup();
            }
        }

        [Benchmark]
        public void Deserialize_10000_Messages()
        {
            var channel = new BenchmarkMqttChannel(_serializedPacket);
            var reader = new MqttChannelAdapter(channel, new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535)), new MqttNetEventLogger());

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

            public Task WriteAsync(ArraySegment<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
