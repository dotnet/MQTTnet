// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Samples.Helpers;

namespace MQTTnet.Samples.Client;

public static class Client_Connection_Samples
{
    public static async Task Connect_Client()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker.
         *
         * Always dispose the client when it is no longer used.
         * The default version of MQTT is 3.1.1.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            // Use builder classes where possible in this project.
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .Build();

            // This will throw an exception if the server is not available.
            // The result from this message returns additional data which was sent 
            // from the server. Please refer to the MQTT protocol specification for details.
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

            response.DumpToConsole();
            
            // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
            // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
            var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder()
                .Build();
            
            await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
        }
    }

    public static async Task Connect_Client_Using_MQTTv5()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker using MQTTv5.
         * 
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build();

            // In MQTTv5 the response contains much more information.
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

            response.DumpToConsole();
        }
    }
    
    public static async Task Connect_Client_With_TLS_Encryption()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker with enabled TLS encryption.
         * 
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("test.mosquitto.org", 8883)
                .WithTls(o =>
                {
                    o.SslProtocol = SslProtocols.Tls12; // The default value is determined by the OS. Set manually to force version.
                })
                .Build();

            // In MQTTv5 the response contains much more information.
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

            response.DumpToConsole();
        }
    }
    
    public static async Task Connect_Client_Using_TLS_1_2()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker using TLS 1.2 encryption.
         * 
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("mqtt.fluux.io")
                .WithTls(o =>
                {
                    o.SslProtocol = SslProtocols.Tls12;
                })
                .Build();
            
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");
        }
    }

    public static async Task Connect_Client_Using_WebSockets()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker using a WebSocket connection.
         * 
         * This is a modified version of the sample _Connect_Client_! See other sample for more details.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithWebSocketServer("broker.hivemq.com:8000/mqtt")
                .Build();

            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

            response.DumpToConsole();
        }
    }

    public static async Task Ping_Server()
    {
        /*
         * This sample sends a PINGREQ packet to the server and waits for a reply.
         *
         * This is only supported in METTv5.0.0+.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            // This will throw an exception if the server does not reply.
            await mqttClient.PingAsync(CancellationToken.None);

            Console.WriteLine("The MQTT server replied to the ping request.");
        }
    }

    public static async Task Reconnect_Using_Event()
    {
        /*
         * This sample shows how to reconnect when the connection was dropped.
         * This approach uses one of the events from the client.
         * This approach has a risk of dead locks! Consider using the timer approach (see sample).
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .Build();

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
    }

    public static void Reconnect_Using_Timer()
    {
        /*
         * This sample shows how to reconnect when the connection was dropped.
         * This approach uses a custom Task/Thread which will monitor the connection status.
         * This is the recommended way but requires more custom code!
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .Build();

            _ = Task.Run(async () =>
            {
                // User proper cancellation and no while(true).
                while (true)
                {
                    try
                    {
                        // This code will also do the very first connect! So no call to _ConnectAsync_ is required
                        // in the first place.
                        if (!mqttClient.IsConnected)
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
        }
    }
}