// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable MemberCanBeMadeStatic.Local

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_ASP_NET_Samples
{
    public static Task Start_Server_With_WebSockets_Support()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMqttServer().ConfigureMqttServer(s => s.WithDefaultEndpoint().WithEncryptedEndpoint());
        builder.Services.AddMqttClient();
        builder.Services.AddHostedService<MqttClientController>();

        builder.WebHost.UseKestrel(kestrel =>
        {
            // Need ConfigureMqttServer(s => s.WithDefaultEndpoint().WithEncryptedEndpoint())
            kestrel.ListenMqtt();

            // We can also manually listen to a specific port without ConfigureMqttServer()
            // kestrel.ListenAnyIP(1883, l => l.UseMqtt());  // mqtt over tcp          
            // kestrel.ListenAnyIP(8883, l => l.UseHttps().UseMqtt());   // mqtt over tls over tcp

            // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:5000/mqtt"
            // See code below for URI configuration.
            kestrel.ListenAnyIP(5000); // Default HTTP pipeline
        });

        var app = builder.Build();
        app.MapMqtt("/mqtt");
        app.UseMqttServer<MqttServerController>();
        return app.RunAsync();
    }

    sealed class MqttServerController
    {
        private readonly ILogger<MqttServerController> _logger;

        public MqttServerController(
            MqttServer mqttServer,
            ILogger<MqttServerController> logger)
        {
            _logger = logger;

            mqttServer.ValidatingConnectionAsync += ValidateConnection;
            mqttServer.ClientConnectedAsync += OnClientConnected;
        }

        public Task OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            _logger.LogInformation($"Client '{eventArgs.ClientId}' connected.");
            return Task.CompletedTask;
        }

        public Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
        {
            _logger.LogInformation($"Client '{eventArgs.ClientId}' wants to connect. Accepting!");
            return Task.CompletedTask;
        }
    }

    sealed class MqttClientController : BackgroundService
    {
        private readonly IMqttClientFactory _mqttClientFactory;

        public MqttClientController(IMqttClientFactory mqttClientFactory)
        {
            _mqttClientFactory = mqttClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000);
            using var client = _mqttClientFactory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder().WithConnectionUri("mqtt://127.0.0.1:1883").Build();
            await client.ConnectAsync(options, stoppingToken);
            await client.DisconnectAsync();
        }
    }
}