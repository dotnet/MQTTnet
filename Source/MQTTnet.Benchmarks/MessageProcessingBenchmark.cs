// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Server;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter]
[RankColumn]
[MemoryDiagnoser]
public class MessageProcessingBenchmark : BaseBenchmark
{
    MqttApplicationMessage _message;
    IMqttClient _mqttClient;
    MqttServer _mqttServer;

    [Benchmark]
    public void Send_10000_Messages()
    {
        for (var i = 0; i < 10000; i++)
        {
            _mqttClient.PublishAsync(_message).GetAwaiter().GetResult();
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        var serverOptions = new MqttServerOptionsBuilder().Build();

        var serverFactory = new MqttServerFactory();
        _mqttServer = serverFactory.CreateMqttServer(serverOptions);
        var clientFactory = new MqttClientFactory();
        _mqttClient = clientFactory.CreateMqttClient();

        _mqttServer.StartAsync().GetAwaiter().GetResult();

        var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();

        _mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();

        _message = new MqttApplicationMessageBuilder().WithTopic("A").Build();
    }
}