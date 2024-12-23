// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[RPlotExporter]
[RankColumn]
[MemoryDiagnoser]
public class MessageProcessingBenchmark : BaseBenchmark
{
    IMqttClient _mqttClient;
    MqttServer _mqttServer;
    string _payload = string.Empty;

    [Params(1 * 1024, 4 * 1024, 8 * 1024)]
    public int PayloadSize { get; set; }

    [Benchmark]
    public async Task Send_1000_Messages()
    {
        for (var i = 0; i < 1000; i++)
        {
            await _mqttClient.PublishStringAsync("A", _payload);
        }
    }

    [GlobalSetup]
    public async Task Setup()
    {
        var serverFactory = new MqttServerFactory();
        var serverOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .Build();
       
        _mqttServer = serverFactory.CreateMqttServer(serverOptions);
        await _mqttServer.StartAsync();

        var clientFactory = new MqttClientFactory();
        _mqttClient = clientFactory.CreateMqttClient();

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost")
            .Build();

        await _mqttClient.ConnectAsync(clientOptions);

        _payload = string.Empty.PadLeft(PayloadSize, '0');
    }
}