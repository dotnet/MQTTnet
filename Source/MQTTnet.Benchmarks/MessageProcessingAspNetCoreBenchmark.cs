// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[RPlotExporter]
[RankColumn]
[MemoryDiagnoser]
public class MessageProcessingAspNetCoreBenchmark : BaseBenchmark
{
    IMqttClient _mqttClient;
    string _payload = string.Empty;

    [Params(1 * 1024, 4 * 1024, 8 * 1024)]
    public int PayloadSize { get; set; }

    [Benchmark]
    public async Task Send_1000_Messages_AspNetCore()
    {
        for (var i = 0; i < 1000; i++)
        {
            await _mqttClient.PublishStringAsync("A", _payload);
        }
    }

    [GlobalSetup]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMqttServer(s => s.WithDefaultEndpoint());
        builder.Services.AddMqttClient();
        builder.WebHost.UseKestrel(k => k.ListenMqtt());

        var app = builder.Build();
        await app.StartAsync();

        _mqttClient = app.Services.GetRequiredService<IMqttClientFactory>().CreateMqttClient();
        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost")
            .Build();

        await _mqttClient.ConnectAsync(clientOptions);

        _payload = string.Empty.PadLeft(PayloadSize, '0');
    }
}