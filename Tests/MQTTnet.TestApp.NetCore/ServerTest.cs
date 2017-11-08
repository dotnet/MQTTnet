using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MQTTnet.TestApp.NetCore
{
    public static class ServerTest
    {
        public static Task RunAsync()
        {
            try
            {
                var services = new ServiceCollection()
                    .AddMqttServer()
                    .AddLogging();

                services.Configure<MqttServerOptions>(options =>
                {
                    options.ConnectionValidator = p =>
                    {
                        if (p.ClientId == "SpecialClient")
                        {
                            if (p.Username != "USER" || p.Password != "PASS")
                            {
                                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }
                        }

                        return MqttConnectReturnCode.ConnectionAccepted;
                    };

                    options.Storage = new RetainedMessageHandler();

                    // Extend the timestamp for all messages from clients.
                    options.ApplicationMessageInterceptor = context =>
                    {
                        if (MqttTopicFilterComparer.IsMatch(context.ApplicationMessage.Topic, "/myTopic/WithTimestamp/#"))
                        {
        // Replace the payload with the timestamp. But also extending a JSON 
        // based payload with the timestamp is a suitable use case.
        context.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));
                        }
                    };
                    // Protect several topics from being subscribed from every client.
                    options.SubscriptionsInterceptor = context =>
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
                    };
                });

                var serviceProvider = services.BuildServiceProvider();
                serviceProvider.GetRequiredService<ILoggerFactory>().AddConsole();

                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.DefaultEndpointOptions.IsEnabled = true;
                //options.TlsEndpointOptions.IsEnabled = false;

                var mqttServer = new MqttFactory(serviceProvider).CreateMqttServer();
                mqttServer.ClientDisconnected += (s, e) =>
                {
                    Console.Write("Client disconnected event fired.");
                };

                mqttServer.StartAsync();

                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                mqttServer.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
            return Task.FromResult(0);
        }
    }
}
