// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class MessageProcessingMqttConnectionContextBenchmark : BaseBenchmark
    {
        IWebHost _host;
        IMqttClient _mqttClient;
        MqttApplicationMessage _message;

        [GlobalSetup]
        public void Setup()
        {
            _host = WebHost.CreateDefaultBuilder()
                   .UseKestrel(o => o.ListenAnyIP(1883, l => l.UseMqtt()))
                   .ConfigureServices(services =>
                   {
                       services.AddMqttServer();
                       services.AddMqttClient().UseAspNetCoreMqttClientAdapterFactory();
                   })
                   .Build();

            var factory = _host.Services.GetRequiredService<IMqttClientFactory>();
            _mqttClient = factory.CreateMqttClient();

            _host.StartAsync().GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            _mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();

            _message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
            _mqttClient.Dispose();

            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
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
