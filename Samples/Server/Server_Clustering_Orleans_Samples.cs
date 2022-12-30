using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTnet.Clustering.Orleans.Server.Services;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                            // By default, if no other topic route is matched, the message will be relayed to all nodes in the cluster
                            .DefaultTopicBehavior(topic => topic.RelayAll())

                            // For this topic, route the message directly to the single client that has subscribed to the topic
                            .ForTopic("clients/c2d/{unitId}/{deviceId}/message",
                                topic => topic.RestrictToSingleClient())

                            // For this topic, route the message to many clients, using grains to route to necessary subscribers
                            .ForTopic("clients/d2c/{unitId}/{deviceId}/telemetry/{*topic}",
                                topic => topic.RelayToManyClients("units/{unitId}/{deviceId}/telemetry/{topic}"))

                            // For this topic, route the message to a grain call, using the client id as the grain key
                            .ForTopic("clients/d2c/{unitId}/{deviceId}/app-route/a/{*topic}",
                                topic =>
                                    topic.RestrictToGrain<IMyGrain>(async context =>
                                    {
                                        await context.Grain.SendMessage(new GrainApplicationMessage
                                        {
                                            Topic = $"units/{context.Arguments["unitId"]}:{context.Arguments["deviceId"]}/telemetry/{context.Arguments["topic"]}",
                                            Data = context.Message.Payload
                                        });
                                    }))

                            // For this topic, route the message to a grain call, using a formatted client id for the grain key
                            .ForTopic("clients/d2c/{unitId}/{deviceId}/app-route/b/{*topic}",
                                topic =>
                                    topic.RestrictToGrain<IMyGrain>(b => $"client_{b.ClientId}", async context =>
                                    {
                                        await context.Grain.SendMessage(new GrainApplicationMessage
                                        {
                                            Topic = $"units/{context.Arguments["unitId"]}:{context.Arguments["deviceId"]}/telemetry/{context.Arguments["topic"]}",
                                            Data = context.Message.Payload
                                        });
                                    }))

                            // For this topic, route the message to a grain call, using a formatted client id for the grain key
                            .ForTopic("clients/d2c/{unitId}/{deviceId}/app-route/c/{*topic}",
                                topic =>
                                    topic.RestrictToGrain<IMyGrain>(b => $"{b.SessionItems["type"]}:{b.SessionItems["name"]}", async context =>
                                    {
                                        await context.Grain.SendMessage(new GrainApplicationMessage
                                        {
                                            Topic = $"units/{context.Arguments["unitId"]}:{context.Arguments["deviceId"]}/telemetry/{context.Arguments["topic"]}",
                                            Data = context.Message.Payload
                                        });
                                    }))

                            // For this topic, aggregate messages and route the list to a grain call, using the client id as the grain key
                            .ForTopic("clients/d2c/{unitId}/{deviceId}/app-route/d/{*topic}",
                                topic =>
                                    topic.AggregateToGrain<IMyGrain>(async context =>
                                    {
                                        var messages = new List<GrainApplicationMessage>(context.Items.Count);
                                        foreach (var messageContext in context.Items)
                                        {
                                            messages.Add(
                                                new GrainApplicationMessage
                                                {
                                                    Topic = $"units/{messageContext.Arguments["unitId"]}:{messageContext.Arguments["deviceId"]}/telemetry/{messageContext.Arguments["topic"]}",
                                                    Data = messageContext.Message.Payload
                                                });
                                        }

                                        await context.Grain.SendMessages(messages);
                                    }));
                    });
            }


        }

        public interface IMyGrain : IGrainWithStringKey
        {

            ValueTask SendMessage(GrainApplicationMessage message);

            ValueTask SendMessages(List<GrainApplicationMessage> messages);

        }

        public class GrainApplicationMessage
        {
            [MaybeNull]
            public string Topic { get; set; }

            [MaybeNull]
            public byte[] Data { get; set; }

        }

    }
}
