// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;
using System.Buffers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class MqttTcpChannelBenchmark : BaseBenchmark
{
    IMqttChannel _clientChannel;
    MqttServer _mqttServer;
    IMqttChannel _serverChannel;

    [Benchmark]
    public async Task Send_10000_Chunks()
    {
        var size = 5;
        var iterations = 10000;

        await Task.WhenAll(WriteAsync(iterations, size), ReadAsync(iterations, size));
    }

    [GlobalSetup]
    public void Setup()
    {
        var serverFactory = new MqttServerFactory();
        var tcpServer = new MqttTcpServerAdapter();
        tcpServer.ClientHandler += args =>
        {
            _serverChannel = (IMqttChannel)args.GetType().GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(args);

            return Task.CompletedTask;
        };

        var serverOptions = new MqttServerOptionsBuilder().Build();
        _mqttServer = serverFactory.CreateMqttServer(serverOptions, new[] { tcpServer }, new MqttNetEventLogger());


        _mqttServer.StartAsync().GetAwaiter().GetResult();

        var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();

        var tcpOptions = (MqttClientTcpOptions)clientOptions.ChannelOptions;
        _clientChannel = new MqttTcpChannel(new MqttClientOptions { ChannelOptions = tcpOptions });

        _clientChannel.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    async Task ReadAsync(int iterations, int size)
    {
        await Task.Yield();

        var buffer = new byte[size];
        var expected = iterations * size;
        long read = 0;

        while (read < expected)
        {
            var readResult = await _clientChannel.ReadAsync(buffer, 0, size, CancellationToken.None).ConfigureAwait(false);
            read += readResult;
        }
    }

    async Task WriteAsync(int iterations, int size)
    {
        await Task.Yield();

        var buffer = new ReadOnlySequence<byte>(new byte[size]);

        for (var i = 0; i < iterations; i++)
        {
            await _serverChannel.WriteAsync(buffer, true, CancellationToken.None).ConfigureAwait(false);
        }
    }
}