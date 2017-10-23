using System;
using System.Threading.Tasks;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MQTTnet.TestApp.NetCore
{
    public static class ManagedClientTest
    {
        public static async Task RunAsync()
        {
            var services = new ServiceCollection()
                   .AddMqttClient()
                   .AddLogging()
                   .BuildServiceProvider();

            services.GetService<ILoggerFactory>()
                .AddConsole();


            var options = new ManagedMqttClientOptions
            {
                ClientOptions = new MqttClientTcpOptions
                {
                    Server = "broker.hivemq.com",
                    ClientId = "MQTTnetManagedClientTest"
                },

                AutoReconnectDelay = TimeSpan.FromSeconds(1)
            };

            try
            {
                var managedClient = services.GetRequiredService<ManagedMqttClient>();
                managedClient.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine(">> RECEIVED: " + e.ApplicationMessage.Topic);
                };

                await managedClient.EnqueueAsync(new MqttApplicationMessageBuilder().WithTopic("Step").WithPayload("1").Build());
                await managedClient.EnqueueAsync(new MqttApplicationMessageBuilder().WithTopic("Step").WithPayload("2").WithAtLeastOnceQoS().Build());

                await managedClient.StartAsync(options);

                await managedClient.SubscribeAsync(new TopicFilter("xyz", MqttQualityOfServiceLevel.AtMostOnce));

                await managedClient.EnqueueAsync(new MqttApplicationMessageBuilder().WithTopic("Step").WithPayload("3").Build());

                Console.WriteLine("Managed client started.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
