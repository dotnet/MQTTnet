// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Client;
using MQTTnet.Server;
using MqttClient = MQTTnet.Client.MqttClient;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net461)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class MessageProcessingBenchmark
    {
        MqttServer _mqttServer;
        MqttClient _mqttClient;
        MqttApplicationMessage _message;

        [GlobalSetup]
        public void Setup()
        {
            var serverOptions = new MqttServerOptionsBuilder().Build();
            
            var factory = new MqttFactory();
            _mqttServer = factory.CreateMqttServer(serverOptions);
            _mqttClient = factory.CreateMqttClient();

            _mqttServer.StartAsync().GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            _mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();

            _message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();
        }

        [Benchmark]
        public void Send_10000_Messages()
        {
            for (var i = 0; i < 10000; i++)
            {
                _mqttClient.PublishAsync(_message).GetAwaiter().GetResult();
            }
        }
    }
}
