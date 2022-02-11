// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;

namespace MQTTnet.TestApp
{
    public static class ServerTest
    {
        public static void RunEmptyServer()
        {
            var mqttServer = new MqttFactory().CreateMqttServer(new MqttServerOptions());
            mqttServer.StartAsync().GetAwaiter().GetResult();
            
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        public static void RunEmptyServerWithLogging()
        {
            var logger = new MqttNetEventLogger();
            MqttNetConsoleLogger.ForwardToConsole(logger);
           
            var mqttFactory = new MqttFactory(logger);
            var mqttServer = mqttFactory.CreateMqttServer(new MqttServerOptions());
            mqttServer.StartAsync().GetAwaiter().GetResult();

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
        
        public static async Task RunAsync()
        {
            try
            {
                var options = new MqttServerOptions();

                // Extend the timestamp for all messages from clients.
                // Protect several topics from being subscribed from every client.

                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.DefaultEndpointOptions.IsEnabled = true;
                //options.TlsEndpointOptions.IsEnabled = false;

                var mqttServer = new MqttFactory().CreateMqttServer(options);

                const string Filename = "C:\\MQTT\\RetainedMessages.json";
                
                mqttServer.RetainedMessageChangedAsync += e =>
                {
                    var directory = Path.GetDirectoryName(Filename);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(Filename, JsonConvert.SerializeObject(e.StoredRetainedMessages));
                    return Task.FromResult(0);
                };
                
                mqttServer.RetainedMessagesClearedAsync += e =>
                {
                    File.Delete(Filename);
                    return Task.FromResult(0);
                };
                
                mqttServer.LoadingRetainedMessageAsync += e =>
                {
                    List<MqttApplicationMessage> retainedMessages;
                    if (File.Exists(Filename))
                    {
                        var json = File.ReadAllText(Filename);
                        retainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
                    }
                    else
                    {
                        retainedMessages = new List<MqttApplicationMessage>();
                    }

                    e.LoadedRetainedMessages = retainedMessages;

                    return Task.FromResult(0);
                };

                mqttServer.InterceptingPublishAsync += e =>
                {
                    if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "/myTopic/WithTimestamp/#") == MqttTopicFilterCompareResult.IsMatch)
                    {
                        // Replace the payload with the timestamp. But also extending a JSON 
                        // based payload with the timestamp is a suitable use case.
                        e.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));
                    }

                    if (e.ApplicationMessage.Topic == "not_allowed_topic")
                    {
                        e.ProcessPublish = false;
                        e.CloseConnection = true;
                    }

                    return PlatformAbstractionLayer.CompletedTask;
                };
                
                mqttServer.ValidatingConnectionAsync += e =>
                {
                    if (e.ClientId == "SpecialClient")
                    {
                        if (e.Username != "USER" || e.Password != "PASS")
                        {
                            e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        }
                    }

                    return PlatformAbstractionLayer.CompletedTask;
                };

                mqttServer.InterceptingSubscriptionAsync += e =>
                {
                    if (e.TopicFilter.Topic.StartsWith("admin/foo/bar") && e.ClientId != "theAdmin")
                    {
                        e.Response.ReasonCode = MqttSubscribeReasonCode.ImplementationSpecificError;
                    }

                    if (e.TopicFilter.Topic.StartsWith("the/secret/stuff") && e.ClientId != "Imperator")
                    {
                        e.Response.ReasonCode = MqttSubscribeReasonCode.ImplementationSpecificError;
                        e.CloseConnection = true;
                    }

                    return PlatformAbstractionLayer.CompletedTask;
                };
                
                mqttServer.InterceptingPublishAsync += e =>
                {
                    MqttNetConsoleLogger.PrintToConsole(
                        $"'{e.ClientId}' reported '{e.ApplicationMessage.Topic}' > '{Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? new byte[0])}'",
                        ConsoleColor.Magenta);
                    
                    return PlatformAbstractionLayer.CompletedTask;
                };

                //options.ApplicationMessageInterceptor = c =>
                //{
                //    if (c.ApplicationMessage.Payload == null || c.ApplicationMessage.Payload.Length == 0)
                //    {
                //        return;
                //    }

                //    try
                //    {
                //        var content = JObject.Parse(Encoding.UTF8.GetString(c.ApplicationMessage.Payload));
                //        var timestampProperty = content.Property("timestamp");
                //        if (timestampProperty != null && timestampProperty.Value.Type == JTokenType.Null)
                //        {
                //            timestampProperty.Value = DateTime.Now.ToString("O");
                //            c.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(content.ToString());
                //        }
                //    }
                //    catch (Exception)
                //    {
                //    }
                //};

                mqttServer.ClientConnectedAsync += e =>
                {
                    Console.Write("Client disconnected event fired.");
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await mqttServer.StartAsync();

                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                await mqttServer.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }
    }
}
