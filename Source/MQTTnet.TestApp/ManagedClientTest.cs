// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.TestApp
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
                managedClient.ApplicationMessageReceivedAsync += e =>
                {
                    Console.WriteLine(">> RECEIVED: " + e.ApplicationMessage.Topic);
                    return CompletedTask.Instance;
                };

                await managedClient.StartAsync(options);

                await managedClient.EnqueueAsync(topic: "Step", payload: "1");
                await managedClient.EnqueueAsync(topic: "Step", payload: "2", MqttQualityOfServiceLevel.AtLeastOnce);
                
                await managedClient.SubscribeAsync(topic: "xyz", qualityOfServiceLevel: MqttQualityOfServiceLevel.AtMostOnce);
                await managedClient.SubscribeAsync(topic: "abc", qualityOfServiceLevel: MqttQualityOfServiceLevel.AtMostOnce);

                await managedClient.EnqueueAsync(topic: "Step", payload: "3");

                Console.WriteLine("Managed client started.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public sealed class RandomPassword : IMqttClientCredentialsProvider
        {
            public string GetUserName(MqttClientOptions clientOptions)
            {
                return "the_static_user";
            }

            public byte[] GetPassword(MqttClientOptions clientOptions)
            {
                return Guid.NewGuid().ToByteArray();
            }
        }

        public class ClientRetainedMessageHandler : IManagedMqttClientStorage
        {
            const string Filename = @"RetainedMessages.json";

            public Task SaveQueuedMessagesAsync(IList<ManagedMqttApplicationMessage> messages)
            {
                File.WriteAllText(Filename, JsonConvert.SerializeObject(messages));
                return CompletedTask.Instance;
            }

            public Task<IList<ManagedMqttApplicationMessage>> LoadQueuedMessagesAsync()
            {
                IList<ManagedMqttApplicationMessage> retainedMessages;
                if (File.Exists(Filename))
                {
                    var json = File.ReadAllText(Filename);
                    retainedMessages = JsonConvert.DeserializeObject<List<ManagedMqttApplicationMessage>>(json);
                }
                else
                {
                    retainedMessages = new List<ManagedMqttApplicationMessage>();
                }

                return Task.FromResult(retainedMessages);
            }
        }
    }
}
