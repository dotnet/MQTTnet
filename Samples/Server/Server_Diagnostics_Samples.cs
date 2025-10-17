// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable MemberCanBeMadeStatic.Local
public static class Server_Diagnostics_Samples
{
    public static async Task Get_Notified_When_Client_Received_Message()
    {
        /*
         * This sample starts a MQTT server and attaches an event which gets fired whenever
         * a client received and acknowledged a PUBLISH packet.
         */

        var mqttServerFactory = new MqttServerFactory();
        var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();

        using var mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions);
        // Attach the event handler.
        mqttServer.ClientAcknowledgedPublishPacketAsync += e =>
        {
            Console.WriteLine($"Client '{e.ClientId}' acknowledged packet {e.PublishPacket.PacketIdentifier} with topic '{e.PublishPacket.Topic}'");

            // It is also possible to read additional data from the client response. This requires casting the response packet.
            var qos1AcknowledgePacket = e.AcknowledgePacket as MqttPubAckPacket;
            Console.WriteLine($"QoS 1 reason code: {qos1AcknowledgePacket?.ReasonCode}");

            var qos2AcknowledgePacket = e.AcknowledgePacket as MqttPubCompPacket;
            Console.WriteLine($"QoS 2 reason code: {qos2AcknowledgePacket?.ReasonCode}");
            return CompletedTask.Instance;
        };

        await mqttServer.StartAsync();

        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();

        await mqttServer.StopAsync();
    }
}