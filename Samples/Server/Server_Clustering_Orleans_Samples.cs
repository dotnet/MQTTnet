using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTnet.Clustering.Orleans.Server.Services;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Samples.Server
{
    internal static class Server_Clustering_Orleans_Samples
    {

        public static async Task Start_Server_With_Orleans_Clustering()
        {
            var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });

            await host.RunConsoleAsync();
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

        class Startup
        {
            public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, MqttController mqttController)
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
                        server.ValidatingConnectionAsync += mqttController.ValidateConnection;
                        server.ClientConnectedAsync += mqttController.OnClientConnected;
                    })
                    .UseMqttOrleansClustering(clustering =>
                    {
                        clustering
                            .DefaultTopicBehavior(topic => topic.RelayToAll())
                            .ForTopic("clients/c2d/{clientId}/{deviceId}/command", topic => topic.OneToOne());
                    });
            }



            public void ConfigureServices(IServiceCollection services)
            {
                services.AddHostedMqttServer(
                    optionsBuilder =>
                    {
                        optionsBuilder
                            .WithDefaultEndpoint();
                    })
                    .AddMqttServerOrleansClustering();

                services.AddMqttConnectionHandler();
                services.AddConnections();

                services.AddSingleton<MqttController>();
            }
        }

    }
}
