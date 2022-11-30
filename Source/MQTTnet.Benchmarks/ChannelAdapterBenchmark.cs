// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using BenchmarkDotNet.Attributes;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public sealed class ChannelAdapterBenchmark : BaseBenchmark
    {
        MqttChannelAdapter _channelAdapter;
        int _iterations;
        MqttPublishPacket _packet;
        MemoryStream _stream;

        [Benchmark]
        public void Receive_10000_Messages()
        {
            _stream.Position = 0;

            for (var i = 0; i < 10000; i++)
            {
                _channelAdapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            _stream.Position = 0;
        }

        [Benchmark]
        public void Send_10000_Messages()
        {
            _stream.Position = 0;

            for (var i = 0; i < 10000; i++)
            {
                _channelAdapter.SendPacketAsync(_packet, CancellationToken.None).GetAwaiter().GetResult();
            }

            _stream.Position = 0;
        }

        [GlobalSetup]
        public void Setup()
        {
            _packet = new MqttPublishPacket
            {
                Topic = "A"
            };

            var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));

            var serializedPacket = Join(serializer.Encode(_packet).Join());

            _iterations = 10000;

            _stream = new MemoryStream(_iterations * serializedPacket.Length);

            for (var i = 0; i < _iterations; i++)
            {
                _stream.Write(serializedPacket, 0, serializedPacket.Length);
            }

            _stream.Position = 0;

            var channel = new MemoryMqttChannel(_stream);

            _channelAdapter = new MqttChannelAdapter(channel, serializer, null, new MqttNetEventLogger());
        }

        static byte[] Join(params ArraySegment<byte>[] chunks)
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