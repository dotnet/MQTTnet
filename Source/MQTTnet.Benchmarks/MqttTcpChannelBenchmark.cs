// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MQTTnet.Channel;
using MQTTnet.Implementations;
using MQTTnet.Server;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Jobs;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [MemoryDiagnoser]
    public sealed class MqttTcpChannelBenchmark
    {
        MqttServer _mqttServer;
        IMqttChannel _serverChannel;
        IMqttChannel _clientChannel;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new MqttFactory();
            var tcpServer = new MqttTcpServerAdapter();
            tcpServer.ClientHandler += args =>
            {
                _serverChannel =
                    (IMqttChannel)args.GetType().GetField("_channel",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .GetValue(args);

                return Task.CompletedTask;
            };

            var serverOptions = new MqttServerOptionsBuilder().Build();
            _mqttServer = factory.CreateMqttServer(serverOptions, new[] { tcpServer }, new MqttNetEventLogger());

            
            _mqttServer.StartAsync().GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            var tcpOptions = (MqttClientTcpOptions)clientOptions.ChannelOptions;
            _clientChannel = new MqttTcpChannel(new MqttClientOptions { ChannelOptions = tcpOptions });

            _clientChannel.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task Send_10000_Chunks()
        {
            var size = 5;
            var iterations = 10000;

            await Task.WhenAll(WriteAsync(iterations, size), ReadAsync(iterations, size));
        }

        async Task ReadAsync(int iterations, int size)
        {
            await Task.Yield();

            var expected = iterations * size;
            long read = 0;

            while (read < expected)
            {
                var readresult = await _clientChannel.ReadAsync(new byte[size], 0, size, CancellationToken.None).ConfigureAwait(false);
                read += readresult;
            }
        }

        async Task WriteAsync(int iterations, int size)
        {
            await Task.Yield();

            for (var i = 0; i < iterations; i++)
            {
                await _serverChannel.WriteAsync(new byte[size], 0, size, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}