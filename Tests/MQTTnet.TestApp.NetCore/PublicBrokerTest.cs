using MQTTnet.Client;
using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.TestApp.NetCore
{
    public static class PublicBrokerTest
    {
        public static async Task RunAsync()
        {
            // MqttNetConsoleLogger.ForwardToConsole();

            // For most of these connections to work, set output target to Net5.0.

            // Note: TLS13 is only available in Net5.0
            var unsafeTls13 = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
#if NET5_0_OR_GREATER
                SslProtocol = SslProtocols.Tls13,
#else
                SslProtocol = SslProtocols.Tls12,
#endif
                CertificateValidationHandler = (w) => true
            };

            // Also defining TLS12 for server test.mosquitto.org
            // It seems that in Win10, using TLS13 on this server, this exception is raised:
            // "The client and server cannot communicate, because they do not possess a common algorithm."
            var unsafeTls12 = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12,
                CertificateValidationHandler = (w) => true
            };

            
            // mqtt.eclipseprojects.io
            await ExecuteTestAsync("mqtt.eclipseprojects.io TCP",
                    new MqttClientOptionsBuilder().WithTcpServer("mqtt.eclipseprojects.io", 1883)
                        .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("mqtt.eclipseprojects.io WS",
                new MqttClientOptionsBuilder().WithWebSocketServer("mqtt.eclipseprojects.io:80/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("mqtt.eclipseprojects.io WS TLS",
                new MqttClientOptionsBuilder().WithWebSocketServer("mqtt.eclipseprojects.io:443/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTls(unsafeTls13).Build());

            
            // test.mosquitto.org
            await ExecuteTestAsync("test.mosquitto.org TCP",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 1883)
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("test.mosquitto.org TCP - Authenticated",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 1884)
                    .WithCredentials("rw", "readwrite")
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("test.mosquitto.org TCP TLS",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8883)
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTls(unsafeTls12).Build());

            await ExecuteTestAsync("test.mosquitto.org TCP TLS - Authenticated",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8885)
                    .WithCredentials("rw", "readwrite")
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTls(unsafeTls12).Build());

            await ExecuteTestAsync("test.mosquitto.org WS",
                new MqttClientOptionsBuilder().WithWebSocketServer("test.mosquitto.org:8080/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("test.mosquitto.org WS TLS",
                new MqttClientOptionsBuilder().WithWebSocketServer("test.mosquitto.org:8081/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTls(unsafeTls12).Build());

            
            // broker.emqx.io
            await ExecuteTestAsync("broker.emqx.io TCP",
                new MqttClientOptionsBuilder().WithTcpServer("broker.emqx.io", 1883)
                     .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("broker.emqx.io TCP TLS",
                new MqttClientOptionsBuilder().WithTcpServer("broker.emqx.io", 8083)
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTls(unsafeTls13).Build());

            await ExecuteTestAsync("broker.emqx.io WS",
                new MqttClientOptionsBuilder().WithWebSocketServer("broker.emqx.io:8083/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("broker.emqx.io WS TLS",
                new MqttClientOptionsBuilder().WithWebSocketServer("broker.emqx.io:8084/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTls(unsafeTls13).Build());


            // broker.hivemq.com
            await ExecuteTestAsync("broker.hivemq.com TCP",
                new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com", 1883)
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestAsync("broker.hivemq.com WS",
                new MqttClientOptionsBuilder().WithWebSocketServer("broker.hivemq.com:8000/mqtt")
                    .WithProtocolVersion(MqttProtocolVersion.V311).Build());

            // mqtt.swifitch.cz: Does not seem to operate any more

            // cloudmqtt.com: Cannot test because it does not offer a free plan any more.

            Write("Finished.", ConsoleColor.White);
            Console.ReadLine();
        }

        private static async Task ExecuteTestAsync(string name, IMqttClientOptions options)
        {
            try
            {
                Write("Testing '" + name + "'... ", ConsoleColor.Gray);
                var factory = new MqttFactory();
                //factory.UseWebSocket4Net();
                var client = factory.CreateMqttClient();
                var topic = Guid.NewGuid().ToString();

                MqttApplicationMessage receivedMessage = null;
                client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e => receivedMessage = e.ApplicationMessage);

                await client.ConnectAsync(options);
                await client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
                await client.PublishAsync(topic, "Hello_World", MqttQualityOfServiceLevel.AtLeastOnce);

                SpinWait.SpinUntil(() => receivedMessage != null, 5000);

                if (receivedMessage?.Topic != topic || receivedMessage?.ConvertPayloadToString() != "Hello_World")
                {
                    throw new Exception("Message invalid.");
                }

                await client.UnsubscribeAsync(topic);
                await client.DisconnectAsync();

                Write("[OK]\n", ConsoleColor.Green);
            }
            catch (Exception e)
            {
                Write("[FAILED] " + e.Message + "\n", ConsoleColor.Red);
            }
        }

        private static void Write(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
        }

    }
}
