// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable MemberCanBeMadeStatic.Local

using MQTTnet.Internal;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_Intercepting_Samples
{
    public static async Task Intercept_Application_Messages()
    {
        /*
         * This sample starts a simple MQTT server which manipulate all processed application messages.
         * Please see _Server_Simple_Samples_ for more details on how to start a server.
         */

        var mqttServerFactory = new MqttServerFactory();
        var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();

        using var mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions);
        mqttServer.InterceptingPublishAsync += args =>
        {
            // Here we only change the topic of the received application message.
            // but also changing the payload etc. is required. Changing the QoS after
            // transmitting is not supported and makes no sense at all.
            args.ApplicationMessage.Topic += "/manipulated";

            return CompletedTask.Instance;
        };

        await mqttServer.StartAsync();

        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
        await mqttServer.StopAsync();
    }
}