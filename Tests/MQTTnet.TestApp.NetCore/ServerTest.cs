using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;

namespace MQTTnet.TestApp.NetCore
{
    public static class ServerTest
    {
        public static Task RunAsync()
        {
            MqttNetTrace.TraceMessagePublished += (s, e) =>
            {
                Console.WriteLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
                if (e.TraceMessage.Exception != null)
                {
                    Console.WriteLine(e.TraceMessage.Exception);
                }
            };

            try
            {
                var options = new MqttServerOptions
                {
                    ConnectionValidator = p =>
                    {
                        if (p.ClientId == "SpecialClient")
                        {
                            if (p.Username != "USER" || p.Password != "PASS")
                            {
                                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }
                        }

                        return MqttConnectReturnCode.ConnectionAccepted;
                    }
                };

                options.Storage = new RetainedMessageHandler();

                options.ApplicationMessageInterceptor = message =>
                {
                    if (MqttTopicFilterComparer.IsMatch(message.Topic, "/myTopic/WithTimestamp/#"))
                    {
                        // Replace the payload with the timestamp. But also extending a JSON 
                        // based payload with the timestamp is a suitable use case.
                        message.Payload = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));
                    }

                    return message;
                };

                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.DefaultEndpointOptions.IsEnabled = true;
                //options.TlsEndpointOptions.IsEnabled = false;

                var mqttServer = new MqttServerFactory().CreateMqttServer(options);
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
