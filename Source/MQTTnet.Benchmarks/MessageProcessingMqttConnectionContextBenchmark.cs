// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server.Internal.Adapter;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class MessageProcessingMqttConnectionContextBenchmark : BaseBenchmark
    {
        WebApplication _app;
        IMqttClient _aspNetCoreMqttClient;
        IMqttClient _mqttNetMqttClient;
        MqttApplicationMessage _message;

        [Params(1 * 1024, 8 * 1024, 64 * 1024)]
        public int PayloadSize { get; set; }


        [GlobalSetup]
        public async Task Setup()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddMqttServer(s => s.WithDefaultEndpoint()).AddMqttServerAdapter<MqttTcpServerAdapter>().UseMqttNetNullLogger();
            builder.Services.AddMqttClient();
            builder.WebHost.UseKestrel(o =>
            {
                o.ListenAnyIP(1884, l => l.UseMqtt(MqttProtocols.Mqtt));
            });

            _app = builder.Build();
            await _app.StartAsync();

            _message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .WithPayload(new byte[PayloadSize])
                .Build();

            _aspNetCoreMqttClient = _app.Services.GetRequiredService<IMqttClientFactory>().CreateMqttClient();
            var clientOptions = new MqttClientOptionsBuilder().WithConnectionUri("mqtt://localhost:1884").Build();
            await _aspNetCoreMqttClient.ConnectAsync(clientOptions);

            clientOptions = new MqttClientOptionsBuilder().WithConnectionUri("mqtt://localhost:1883").Build();
            _mqttNetMqttClient = new MqttClientFactory().CreateMqttClient(MqttNetNullLogger.Instance);
            await _mqttNetMqttClient.ConnectAsync(clientOptions);
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _aspNetCoreMqttClient.DisconnectAsync();
            _aspNetCoreMqttClient.Dispose();
            await _app.StopAsync();
        }

        [Benchmark(Baseline = true)]
        public async Task AspNetCore_Send_1000_Messages()
        {
            for (var i = 0; i < 1000; i++)
            {
                await _aspNetCoreMqttClient.PublishAsync(_message);
            }
        }

        [Benchmark]
        public async Task MQTTnet_Send_1000_Messages()
        {
            for (var i = 0; i < 1000; i++)
            {
                await _mqttNetMqttClient.PublishAsync(_message);
            }
        }
    }
}
