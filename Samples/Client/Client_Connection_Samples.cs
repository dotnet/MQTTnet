// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Samples.Helpers;
using MQTTnet.Server;

namespace MQTTnet.Samples.Client;

public static class Client_Connection_Samples
{
    const string mosquitto_org = @"
-----BEGIN CERTIFICATE-----
MIIEAzCCAuugAwIBAgIUBY1hlCGvdj4NhBXkZ/uLUZNILAwwDQYJKoZIhvcNAQEL
BQAwgZAxCzAJBgNVBAYTAkdCMRcwFQYDVQQIDA5Vbml0ZWQgS2luZ2RvbTEOMAwG
A1UEBwwFRGVyYnkxEjAQBgNVBAoMCU1vc3F1aXR0bzELMAkGA1UECwwCQ0ExFjAU
BgNVBAMMDW1vc3F1aXR0by5vcmcxHzAdBgkqhkiG9w0BCQEWEHJvZ2VyQGF0Y2hv
by5vcmcwHhcNMjAwNjA5MTEwNjM5WhcNMzAwNjA3MTEwNjM5WjCBkDELMAkGA1UE
BhMCR0IxFzAVBgNVBAgMDlVuaXRlZCBLaW5nZG9tMQ4wDAYDVQQHDAVEZXJieTES
MBAGA1UECgwJTW9zcXVpdHRvMQswCQYDVQQLDAJDQTEWMBQGA1UEAwwNbW9zcXVp
dHRvLm9yZzEfMB0GCSqGSIb3DQEJARYQcm9nZXJAYXRjaG9vLm9yZzCCASIwDQYJ
KoZIhvcNAQEBBQADggEPADCCAQoCggEBAME0HKmIzfTOwkKLT3THHe+ObdizamPg
UZmD64Tf3zJdNeYGYn4CEXbyP6fy3tWc8S2boW6dzrH8SdFf9uo320GJA9B7U1FW
Te3xda/Lm3JFfaHjkWw7jBwcauQZjpGINHapHRlpiCZsquAthOgxW9SgDgYlGzEA
s06pkEFiMw+qDfLo/sxFKB6vQlFekMeCymjLCbNwPJyqyhFmPWwio/PDMruBTzPH
3cioBnrJWKXc3OjXdLGFJOfj7pP0j/dr2LH72eSvv3PQQFl90CZPFhrCUcRHSSxo
E6yjGOdnz7f6PveLIB574kQORwt8ePn0yidrTC1ictikED3nHYhMUOUCAwEAAaNT
MFEwHQYDVR0OBBYEFPVV6xBUFPiGKDyo5V3+Hbh4N9YSMB8GA1UdIwQYMBaAFPVV
6xBUFPiGKDyo5V3+Hbh4N9YSMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQEL
BQADggEBAGa9kS21N70ThM6/Hj9D7mbVxKLBjVWe2TPsGfbl3rEDfZ+OKRZ2j6AC
6r7jb4TZO3dzF2p6dgbrlU71Y/4K0TdzIjRj3cQ3KSm41JvUQ0hZ/c04iGDg/xWf
+pp58nfPAYwuerruPNWmlStWAXf0UTqRtg4hQDWBuUFDJTuWuuBvEXudz74eh/wK
sMwfu1HFvjy5Z0iMDU8PUDepjVolOCue9ashlS4EB5IECdSR2TItnAIiIwimx839
LdUdRudafMu5T5Xma182OC0/u/xRlEm+tvKGGmfFcN0piqVl8OrSPBgIlb+1IKJE
m/XriWr/Cq4h/JfB7NTsezVslgkBaoU=
-----END CERTIFICATE-----
";

    public static async Task Clean_Disconnect()
    {
        /*
         * This sample disconnects in a clean way. This will send a MQTT DISCONNECT packet
         * to the server and close the connection afterward.
         *
         * See sample _Connect_Client_ for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();
        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // This will send the DISCONNECT packet. Calling _Dispose_ without DisconnectAsync the
        // connection is closed in a "not clean" way. See MQTT specification for more details.
        await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
    }

    public static async Task Connect()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker.
         *
         * Always dispose the client when it is no longer used.
         * The default version of MQTT is 3.1.1.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        // Use builder classes where possible in this project.
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        // This will throw an exception if the server is not available.
        // The result from this message returns additional data which was sent
        // from the server. Please refer to the MQTT protocol specification for details.
        var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Console.WriteLine("The MQTT client is connected.");

        response.DumpToConsole();

        // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
        // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
        var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

        await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
    }

    public static async Task Connect_Using_Enhanced_Authentication()
    {
        /*
         * This sample uses enhanced authentication (Kerberos) when creating the connection.
         */


        /*
         * Server part...
         */

        var mqttServerFactory = new MqttServerFactory();

        var serverOptions = mqttServerFactory.CreateServerOptionsBuilder().WithDefaultEndpoint().Build();
        var server = mqttServerFactory.CreateMqttServer(serverOptions);

        server.ValidatingConnectionAsync += async args =>
        {
            if (args.AuthenticationMethod == "GS2-KRB5")
            {
                var result = await args.ExchangeEnhancedAuthenticationAsync(null, args.CancellationToken);

                Console.WriteLine($"Received AUTH data from client: {Encoding.UTF8.GetString(result.AuthenticationData)}");

                var authOptions = mqttServerFactory.CreateExchangeExtendedAuthenticationOptionsBuilder().WithAuthenticationData("reply context token").Build();

                result = await args.ExchangeEnhancedAuthenticationAsync(authOptions, args.CancellationToken);

                Console.WriteLine($"Received AUTH data from client: {Encoding.UTF8.GetString(result.AuthenticationData)}");

                args.ResponseAuthenticationData = "outcome of authentication"u8.ToArray();

                // Authentication DONE!
                args.ReasonCode = MqttConnectReasonCode.Success; // Also the default!
            }
            else
            {
                args.ReasonCode = MqttConnectReasonCode.BadAuthenticationMethod;
            }
        };

        await server.StartAsync();

        /*
         * Client part...
         */

        var mqttClientFactory = new MqttClientFactory();

        // Use Kerberos sample from the MQTT RFC.
        var kerberosAuthenticationHandler = new SampleClientKerberosAuthenticationHandler();

        var clientOptions = mqttClientFactory.CreateClientOptionsBuilder()
            .WithTcpServer("localhost")
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithEnhancedAuthenticationHandler(kerberosAuthenticationHandler)
            .WithEnhancedAuthentication("GS2-KRB5")
            .Build();

        var client = mqttClientFactory.CreateMqttClient();

        var result = await client.ConnectAsync(clientOptions);

        Console.WriteLine($"Client connect result: {result.ResultCode}");
    }

    public static async Task Connect_Using_MQTTv5()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker using MQTTv5.
         *
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").WithProtocolVersion(MqttProtocolVersion.V500).Build();

        // In MQTTv5 the response contains much more information.
        var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Console.WriteLine("The MQTT client is connected.");

        response.DumpToConsole();
    }

    public static async Task Connect_Using_TLS_1_2()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker using TLS 1.2 encryption.
         *
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("mqtt.fluux.io")
            .WithTlsOptions(
                o =>
                {
                    // The used public broker sometimes has invalid certificates. This sample accepts all
                    // certificates. This should not be used in live environments.
                    o.WithCertificateValidationHandler(_ => true);

                    // The default value is determined by the OS. Set manually to force version.
                    o.WithSslProtocols(SslProtocols.Tls12);
                })
            .Build();

        using var timeout = new CancellationTokenSource(5000);
        await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);

        Console.WriteLine("The MQTT client is connected.");
    }

    public static async Task Connect_Using_TLS_Encryption()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker with enabled TLS encryption.
         *
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8883)
            .WithTlsOptions(
                o => o.WithCertificateValidationHandler(
                    // The used public broker sometimes has invalid certificates. This sample accepts all
                    // certificates. This should not be used in live environments.
                    _ => true))
            .Build();

        // In MQTTv5 the response contains much more information.
        using var timeout = new CancellationTokenSource(5000);
        var response = await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);

        Console.WriteLine("The MQTT client is connected.");

        response.DumpToConsole();
    }

    public static async Task Connect_Using_TLS_With_CA_File()
    {
        var mqttFactory = new MqttClientFactory();

        var caChain = new X509Certificate2Collection();
        caChain.ImportFromPem(mosquitto_org); // from https://test.mosquitto.org/ssl/mosquitto.org.crt

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 8883)
            .WithTlsOptions(new MqttClientTlsOptionsBuilder().WithTrustChain(caChain).Build())
            .Build();

        var connAck = await mqttClient.ConnectAsync(mqttClientOptions);
        Console.WriteLine("Connected to test.moquitto.org:8883 with CaFile mosquitto.org.crt: " + connAck.ResultCode);
    }

    public static async Task Connect_Using_WebSockets()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker using a WebSocket connection.
         *
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("broker.hivemq.com:8000/mqtt")).Build();

        var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Console.WriteLine("The MQTT client is connected.");

        response.DumpToConsole();
    }

    public static async Task Connect_With_Amazon_AWS()
    {
        /*
         * This sample creates a simple MQTT client and connects to an Amazon Web Services broker.
         *
         * The broker requires special settings which are set here.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("amazon.web.services.broker")
            // Disabling packet fragmentation is very important!
            .WithoutPacketFragmentation()
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Console.WriteLine("The MQTT client is connected.");

        await mqttClient.DisconnectAsync();
    }

    public static async Task Disconnect_Clean()
    {
        /*
         * This sample disconnects from the server with sending a DISCONNECT packet.
         * This way of disconnecting is treated as a clean disconnect which will not
         * trigger sending the last will etc.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // Calling _DisconnectAsync_ will send a DISCONNECT packet before closing the connection.
        // Using a reason code requires MQTT version 5.0.0!
        await mqttClient.DisconnectAsync(MqttClientDisconnectOptionsReason.ImplementationSpecificError);
    }

    public static async Task Disconnect_Non_Clean()
    {
        /*
         * This sample disconnects from the server without sending a DISCONNECT packet.
         * This way of disconnecting is treated as a non-clean disconnect which will
         * trigger sending the last will etc.
         */

        var mqttFactory = new MqttClientFactory();

        var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // Calling _Dispose_ or use of a _using_ statement will close the transport connection
        // without sending a DISCONNECT packet to the server.
        mqttClient.Dispose();
    }

    public static async Task Inspect_Certificate_Validation_Errors()
    {
        /*
         * This sample prints the certificate information while connection. This data can be used to decide whether a connection is secure or not
         * including the reason for that status.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("mqtt.fluux.io", 8883)
            .WithTlsOptions(
                o =>
                {
                    o.WithCertificateValidationHandler(
                        eventArgs =>
                        {
                            eventArgs.Certificate.Subject.DumpToConsole();
                            eventArgs.Certificate.GetExpirationDateString().DumpToConsole();
                            eventArgs.Chain.ChainPolicy.RevocationMode.DumpToConsole();
                            eventArgs.Chain.ChainStatus.DumpToConsole();
                            eventArgs.SslPolicyErrors.DumpToConsole();
                            return true;
                        });
                })
            .Build();

        // In MQTTv5 the response contains much more information.
        using var timeout = new CancellationTokenSource(5000);
        await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);
    }

    public static async Task Ping_Server()
    {
        /*
         * This sample sends a PINGREQ packet to the server and waits for a reply.
         *
         * This is only supported in MQTTv5.0.0+.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // This will throw an exception if the server does not reply.
        await mqttClient.PingAsync(CancellationToken.None);

        Console.WriteLine("The MQTT server replied to the ping request.");
    }

    public static async Task Reconnect_Using_Event()
    {
        /*
         * This sample shows how to reconnect when the connection was dropped.
         * This approach uses one of the events from the client.
         * This approach has a risk of deadlocks! Consider using the timer approach (see sample).
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        mqttClient.DisconnectedAsync += async e =>
        {
            if (e.ClientWasConnected)
            {
                // Use the current options as the new options.
                await mqttClient.ConnectAsync(mqttClient.Options);
            }
        };

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
    }

    public static void Reconnect_Using_Timer()
    {
        /*
         * This sample shows how to reconnect when the connection was dropped.
         * This approach uses a custom Task/Thread which will monitor the connection status.
         * This is the recommended way but requires more custom code!
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        _ = Task.Run(
            async () =>
            {
                // User proper cancellation and no while(true).
                while (true)
                {
                    try
                    {
                        // This code will also do the very first connect! So no call to _ConnectAsync_ is required in the first place.
                        if (!await mqttClient.TryPingAsync())
                        {
                            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                            // Subscribe to topics when session is clean etc.
                            Console.WriteLine("The MQTT client is connected.");
                        }
                    }
                    catch
                    {
                        // Handle the exception properly (logging etc.).
                    }
                    finally
                    {
                        // Check the connection state every 5 seconds and perform a reconnect if required.
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }
            });

        Console.WriteLine("Press <Enter> to exit");
        Console.ReadLine();
    }

    public static async Task Timeout()
    {
        /*
         * This sample creates a simple MQTT client and connects to an invalid broker using a timeout.
         *
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").Build();

        try
        {
            using var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await mqttClient.ConnectAsync(mqttClientOptions, timeoutToken.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Timeout while connecting.");
        }
    }

    class SampleClientKerberosAuthenticationHandler : IMqttEnhancedAuthenticationHandler
    {
        public async Task HandleEnhancedAuthenticationAsync(MqttEnhancedAuthenticationEventArgs eventArgs)
        {
            if (eventArgs.AuthenticationMethod != "GS2-KRB5")
            {
                throw new InvalidOperationException("Wrong authentication method");
            }

            var sendOptions = new SendMqttEnhancedAuthenticationDataOptions
            {
                Data = "initial context token"u8.ToArray()
            };

            await eventArgs.SendAsync(sendOptions);

            var response = await eventArgs.ReceiveAsync(CancellationToken.None);

            Console.WriteLine($"Received AUTH data from server: {Encoding.UTF8.GetString(response.AuthenticationData)}");

            // No further data is required, but we have to fulfil the exchange.
            sendOptions = new SendMqttEnhancedAuthenticationDataOptions
            {
                Data = []
            };

            await eventArgs.SendAsync(sendOptions, CancellationToken.None);
        }
    }
}