using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Internal;

namespace MQTTnet.TestApp.NetCore
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
                var options = new MqttServerOptions
                {
                    Storage = new RetainedMessageHandler()
                };

                // Extend the timestamp for all messages from clients.
                // Protect several topics from being subscribed from every client.

                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.DefaultEndpointOptions.IsEnabled = true;
                //options.TlsEndpointOptions.IsEnabled = false;

                var mqttServer = new MqttFactory().CreateMqttServer(options);

                mqttServer.InterceptingClientPublishAsync += e =>
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
                
                mqttServer.ValidatingClientConnectionAsync += e =>
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

                mqttServer.InterceptingClientSubscriptionAsync += e =>
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
                
                mqttServer.InterceptingClientPublishAsync += e =>
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
