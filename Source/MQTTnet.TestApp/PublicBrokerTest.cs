// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.TestApp
{
    public static class PublicBrokerTest
    {
        public static async Task RunAsync()
        {
#if NET5_0_OR_GREATER
            // TLS13 is only available in Net5.0
            var unsafeTls13 = new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls13,
                // Don't use this in production code. This handler simply allows any invalid certificate to work.
                AllowUntrustedCertificates = true,
                IgnoreCertificateChainErrors = true,
                CertificateValidationHandler = _ => true
            };
#endif
            // Also defining TLS12 for servers that don't seem no to support TLS13.
            var unsafeTls12 = new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12,
                // Don't use this in production code. This handler simply allows any invalid certificate to work.
                AllowUntrustedCertificates = true,
                IgnoreCertificateChainErrors = true,
                CertificateValidationHandler = _ => true
            };

            // mqtt.eclipseprojects.io
            await ExecuteTestsAsync(
                "mqtt.eclipseprojects.io TCP",
                new MqttClientOptionsBuilder().WithTcpServer("mqtt.eclipseprojects.io", 1883).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "mqtt.eclipseprojects.io WS",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("mqtt.eclipseprojects.io:80/mqtt")).WithProtocolVersion(MqttProtocolVersion.V311).Build());

#if NET5_0_OR_GREATER
            await ExecuteTestsAsync("mqtt.eclipseprojects.io WS TLS13",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("mqtt.eclipseprojects.io:443/mqtt"))
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls13).Build());

#endif

            // test.mosquitto.org
            await ExecuteTestsAsync(
                "test.mosquitto.org TCP",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 1883).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "test.mosquitto.org TCP - Authenticated",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 1884).WithCredentials("rw", "readwrite").WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "test.mosquitto.org TCP TLS12",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8883).WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls12).Build());

#if NET5_0_OR_GREATER
            await ExecuteTestsAsync("test.mosquitto.org TCP TLS13",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8883)
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls13).Build());
#endif

            await ExecuteTestsAsync(
                "test.mosquitto.org TCP TLS12 - Authenticated",
                new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8885)
                    .WithCredentials("rw", "readwrite")
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTlsOptions(unsafeTls12)
                    .Build());

            await ExecuteTestsAsync(
                "test.mosquitto.org WS",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("test.mosquitto.org:8080/mqtt")).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "test.mosquitto.org WS TLS12",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("test.mosquitto.org:8081/mqtt")).WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls12).Build());

            // broker.emqx.io
            await ExecuteTestsAsync(
                "broker.emqx.io TCP",
                new MqttClientOptionsBuilder().WithTcpServer("broker.emqx.io", 1883).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "broker.emqx.io TCP TLS12",
                new MqttClientOptionsBuilder().WithTcpServer("broker.emqx.io", 8883).WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls12).Build());

#if NET5_0_OR_GREATER
            await ExecuteTestsAsync("broker.emqx.io TCP TLS13",
                new MqttClientOptionsBuilder().WithTcpServer("broker.emqx.io", 8883)
                    .WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls13).Build());
#endif

            await ExecuteTestsAsync(
                "broker.emqx.io WS",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("broker.emqx.io:8083/mqtt")).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "broker.emqx.io WS TLS12",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("broker.emqx.io:8084/mqtt")).WithProtocolVersion(MqttProtocolVersion.V311).WithTlsOptions(unsafeTls12).Build());

            // broker.hivemq.com
            await ExecuteTestsAsync(
                "broker.hivemq.com TCP",
                new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com", 1883).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            await ExecuteTestsAsync(
                "broker.hivemq.com WS",
                new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("broker.hivemq.com:8000/mqtt")).WithProtocolVersion(MqttProtocolVersion.V311).Build());

            // mqtt.swifitch.cz: Does not seem to operate any more
            // cloudmqtt.com: Cannot test because it does not offer a free plan any more.

            Write("Finished.", ConsoleColor.White);
            Console.ReadLine();
        }

        static async Task ExecuteTestsAsync(string name, MqttClientOptions options)
        {
            options.ProtocolVersion = MqttProtocolVersion.V311;
            await ExecuteTestAsync(name + " V3.1.1", options);

            options.ProtocolVersion = MqttProtocolVersion.V500;
            await ExecuteTestAsync(name + " V5.0.0", options);
        }

        static async Task ExecuteTestAsync(string name, MqttClientOptions options)
        {
            try
            {
                Write("Testing '" + name + "'... ", ConsoleColor.Gray);

                var factory = new MqttClientFactory();

                using (var client = factory.CreateMqttClient())
                {
                    MqttApplicationMessage receivedMessage = null;
                    client.ApplicationMessageReceivedAsync += e =>
                    {
                        receivedMessage = e.ApplicationMessage;
                        return CompletedTask.Instance;
                    };

                    await client.ConnectAsync(options).ConfigureAwait(false);

                    var topic = Guid.NewGuid().ToString();
                    await client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce).ConfigureAwait(false);
                    await client.PublishStringAsync(topic, "Hello_World", MqttQualityOfServiceLevel.AtLeastOnce).ConfigureAwait(false);

                    SpinWait.SpinUntil(() => receivedMessage != null, 5000);

                    if (receivedMessage?.Topic != topic || receivedMessage?.ConvertPayloadToString() != "Hello_World")
                    {
                        throw new Exception("Message invalid.");
                    }

                    await client.UnsubscribeAsync(topic).ConfigureAwait(false);
                    await client.DisconnectAsync().ConfigureAwait(false);
                }

                Write("[OK]\n", ConsoleColor.Green);
            }
            catch (Exception exception)
            {
                Write("[FAILED]" + Environment.NewLine, ConsoleColor.Red);
                Write(exception + Environment.NewLine, ConsoleColor.Red);
            }
        }

        static void Write(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
        }
    }
}