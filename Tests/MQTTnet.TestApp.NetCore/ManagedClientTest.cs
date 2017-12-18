using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.ManagedClient;
using MQTTnet.Protocol;

namespace MQTTnet.TestApp.NetCore
{
    public static class ManagedClientTest
    {
        public static async Task RunAsync()
        {
            var ms = new ClientRetainedMessageHandler();
            
            var options = new ManagedMqttClientOptions
            {
                ClientOptions = new MqttClientOptions
                {
                    ClientId = "MQTTnetManagedClientTest",
                    Credentials = new RandomPassword(),
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "broker.hivemq.com"
                    }
                },

                AutoReconnectDelay = TimeSpan.FromSeconds(1),
                Storage = ms
            };

            try
            {
                var managedClient = new MqttFactory().CreateManagedMqttClient();
                managedClient.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine(">> RECEIVED: " + e.ApplicationMessage.Topic);
                };

                await managedClient.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("Step").WithPayload("1").Build());
                await managedClient.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("Step").WithPayload("2").WithAtLeastOnceQoS().Build());

                await managedClient.StartAsync(options);

                await managedClient.SubscribeAsync(new TopicFilter("xyz", MqttQualityOfServiceLevel.AtMostOnce));
                await managedClient.SubscribeAsync(new TopicFilter("abc", MqttQualityOfServiceLevel.AtMostOnce));
                
                await managedClient.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("Step").WithPayload("3").Build());

                Console.WriteLine("Managed client started.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public class RandomPassword : IMqttClientCredentials
        {
            public string Password
            {
                get
                {
                    return Guid.NewGuid().ToString(); // The random password.
                }
            }

            public string Username => "the_static_user";
        }

        public class ClientRetainedMessageHandler : IManagedMqttClientStorage
        {
            private const string Filename = @"RetainedMessages.json";

            public Task SaveQueuedMessagesAsync(IList<MqttApplicationMessage> messages)
            {
                File.WriteAllText(Filename, JsonConvert.SerializeObject(messages));
                return Task.FromResult(0);
            }

            public Task<IList<MqttApplicationMessage>> LoadQueuedMessagesAsync()
            {
                IList<MqttApplicationMessage> retainedMessages;
                if (File.Exists(Filename))
                {
                    var json = File.ReadAllText(Filename);
                    retainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
                }
                else
                {
                    retainedMessages = new List<MqttApplicationMessage>();
                }

                return Task.FromResult(retainedMessages);
            }
        }
    }
}
