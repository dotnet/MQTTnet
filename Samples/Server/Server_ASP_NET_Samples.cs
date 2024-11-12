// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable MemberCanBeMadeStatic.Local

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_ASP_NET_Samples
{
    public static Task Start_Server_With_WebSockets_Support()
    {
        /*
         * This sample starts a minimal ASP.NET Webserver including a hosted MQTT server.
         */
        var host = Host.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseKestrel(
                        o =>
                        {
                            // This will allow MQTT connections based on TCP port 1883.
                            o.ListenAnyIP(1883, l => l.UseMqtt());

                            // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:5000/mqtt"
                            // See code below for URI configuration.
                            o.ListenAnyIP(5000); // Default HTTP pipeline
                        });

                    webBuilder.UseStartup<Startup>();
                });

        return host.RunConsoleAsync();
    }

    sealed class MqttController
    {
        public MqttController()
        {
            // Inject other services via constructor.
        }

        public Task OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            Console.WriteLine($"Client '{eventArgs.ClientId}' connected.");
            return Task.CompletedTask;
        }


        public Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
        {
            Console.WriteLine($"Client '{eventArgs.ClientId}' wants to connect. Accepting!");
            return Task.CompletedTask;
        }
    }

    sealed class Startup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, MqttController mqttController)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapMqtt("/mqtt"));

            app.UseMqttServer(
                server =>
                {
                    /*
                     * Attach event handlers etc. if required.
                     */

                    server.ValidatingConnectionAsync += mqttController.ValidateConnection;
                    server.ClientConnectedAsync += mqttController.OnClientConnected;
                });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMqttServer(optionsBuilder =>
            {
                optionsBuilder.WithDefaultEndpoint();
            });

            services.AddSingleton<MqttController>();
        }
    }
}