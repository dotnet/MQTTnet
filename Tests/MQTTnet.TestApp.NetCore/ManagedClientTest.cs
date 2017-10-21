using System;
using System.Threading.Tasks;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.TestApp.NetCore
{
    public static class ManagedClientTest
    {
        public static async Task RunAsync()
        {
            MqttNetTrace.TraceMessagePublished += (s, e) =>
            {
                Console.WriteLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
                if (e.TraceMessage.Exception != null)
                {
                    Console.WriteLine(e.TraceMessage.Exception);
                }
            };

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
                var managedClient = new MqttClientFactory().CreateManagedMqttClient();
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
