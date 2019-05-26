using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.TestApp.NetCore
{
    public static class ServerTest
    {
        public static void RunEmptyServer()
        {
            var mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.StartAsync(new MqttServerOptions()).GetAwaiter().GetResult();

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        public static async Task RunAsync()
        {
            try
            {
                var options = new MqttServerOptions
                {
                    ConnectionValidator = new MqttServerConnectionValidatorDelegate(p =>
                    {
                        if (p.ClientId == "SpecialClient")
                        {
                            var password = Encoding.UTF8.GetString(p.Password);
                            if (p.Username != "USER" || password != "PASS")
                            {
                                p.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }
                        }
                    }),

                    Storage = new RetainedMessageHandler(),

                    ApplicationMessageInterceptor = new MqttServerApplicationMessageInterceptorDelegate(context =>
                    {
                        if (MqttTopicFilterComparer.IsMatch(context.ApplicationMessage.Topic, "/myTopic/WithTimestamp/#"))
                        {
                            // Replace the payload with the timestamp. But also extending a JSON 
                            // based payload with the timestamp is a suitable use case.
                            context.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));
                        }

                        if (context.ApplicationMessage.Topic == "not_allowed_topic")
                        {
                            context.AcceptPublish = false;
                            context.CloseConnection = true;
                        }
                    }),

                    SubscriptionInterceptor = new MqttServerSubscriptionInterceptorDelegate(context =>
                    {
                        if (context.TopicFilter.Topic.StartsWith("admin/foo/bar") && context.ClientId != "theAdmin")
                        {
                            context.AcceptSubscription = false;
                        }

                        if (context.TopicFilter.Topic.StartsWith("the/secret/stuff") && context.ClientId != "Imperator")
                        {
                            context.AcceptSubscription = false;
                            context.CloseConnection = true;
                        }
                    })
                };

                // Extend the timestamp for all messages from clients.
                // Protect several topics from being subscribed from every client.

                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.DefaultEndpointOptions.IsEnabled = true;
                //options.TlsEndpointOptions.IsEnabled = false;

                var mqttServer = new MqttFactory().CreateMqttServer();

                mqttServer.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    MqttNetConsoleLogger.PrintToConsole(
                        $"'{e.ClientId}' reported '{e.ApplicationMessage.Topic}' > '{Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? new byte[0])}'",
                        ConsoleColor.Magenta);
                });

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

                mqttServer.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e =>
                {
                    Console.Write("Client disconnected event fired.");
                });

                await mqttServer.StartAsync(options);

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
