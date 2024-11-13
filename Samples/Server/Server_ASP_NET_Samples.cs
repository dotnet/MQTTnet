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
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_ASP_NET_Samples
{
    public static Task Start_Server_With_WebSockets_Support()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMqttServer();
        builder.Services.AddSingleton<MqttController>();

        builder.WebHost.UseKestrel(kestrel =>
        {
            // mqtt over tcp
            kestrel.ListenAnyIP(1883, l => l.UseMqtt());

            // mqtt over tls over tcp
            kestrel.ListenAnyIP(1884, l => l.UseHttps().UseMqtt());

            // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:5000/mqtt"
            // See code below for URI configuration.
            kestrel.ListenAnyIP(5000); // Default HTTP pipeline
        });

        var app = builder.Build();
        app.MapMqtt("/mqtt");
        app.UseMqttServer<MqttController>();
        return app.RunAsync();
    }

    sealed class MqttController
    {
        private readonly ILogger<MqttController> _logger;

        public MqttController(
            MqttServer mqttServer,
            ILogger<MqttController> logger)
        {
            mqttServer.ValidatingConnectionAsync += ValidateConnection;
            mqttServer.ClientConnectedAsync += OnClientConnected;
            _logger = logger;
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
}