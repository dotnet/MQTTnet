using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;

namespace MQTTnet.TestMqttClient
{
    public static class Program
    {
        public static void Main(string[] arguments)
        {
            Task.Run(() => Run(arguments)).Wait();
        }

        private static async Task Run(string[] arguments)
        {
            MqttTrace.TraceMessagePublished += (s, e) =>
            {
                Console.WriteLine($">> [{e.ThreadId}] [{e.Source}] [{e.Level}]: {e.Message}");
                if (e.Exception != null)
                {
                    Console.WriteLine(e.Exception);
                }
            };
                       
            try
            {
                var options = new MqttClientOptions
                {
                    Server = "localhost",
                    ClientId = "XYZ",
                    CleanSession = true
                };

                var client = new MqttClientFactory().CreateMqttClient(options);
                client.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                };

                client.Connected += async (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    await client.SubscribeAsync(new List<TopicFilter>
                    {
                        new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce)
                    });

                    Console.WriteLine("### SUBSCRIBED ###");
                };

                client.Disconnected += async (s, e) => 
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await client.ConnectAsync();
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    await client.ConnectAsync();
                }
                catch
                {
                    Console.WriteLine("### CONNECTING FAILED ###");
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                while (true)
                {
                    Console.ReadLine();

                    var applicationMessage = new MqttApplicationMessage(
                        "A/B/C",
                        Encoding.UTF8.GetBytes("Hello World"),
                        MqttQualityOfServiceLevel.AtLeastOnce,
                        false
                    );

                    await client.PublishAsync(applicationMessage);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
