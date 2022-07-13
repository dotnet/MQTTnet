// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;

namespace MQTTnet.Samples.Server;

public static class Server_Asp_Net_Samples
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

    sealed class Startup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapConnectionHandler<MqttConnectionHandler>(
                        "/mqtt",
                        httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                            protocolList => protocolList.FirstOrDefault() ?? string.Empty);
                });

            app.UseMqttServer(
                server =>
                {
                    /*
                     * Attach event handlers etc. if required.
                     */

                    server.ValidatingConnectionAsync += e =>
                    {
                        Console.WriteLine($"Client '{e.ClientId}' wants to connect. Accepting!");
                        return Task.CompletedTask;
                    };

                    server.ClientConnectedAsync += e =>
                    {
                        Console.WriteLine($"Client '{e.ClientId}' connected.");
                        return Task.CompletedTask;
                    };
                });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedMqttServer(
                optionsBuilder =>
                {
                    optionsBuilder.WithDefaultEndpoint();
                });

            services.AddMqttConnectionHandler();
            services.AddConnections();
        }
    }
}