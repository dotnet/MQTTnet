// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Samples.Diagnostics;

public static class PackageInspection_Samples
{
    public static async Task Inspect_Outgoing_Package()
    {
        /*
         * This sample covers the inspection of outgoing packages from the client.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = mqttFactory.CreateClientOptionsBuilder()
            .WithTcpServer("broker.hivemq.com")
            .Build();

        mqttClient.InspectPacketAsync += OnInspectPacket;

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Console.WriteLine("MQTT client is connected.");

        var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder()
            .Build();

        await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
    }

    static Task OnInspectPacket(InspectMqttPacketEventArgs eventArgs)
    {
        if (eventArgs.Direction == MqttPacketFlowDirection.Inbound)
        {
            Console.WriteLine($"IN: {Convert.ToBase64String(eventArgs.Buffer)}");
        }
        else
        {
            Console.WriteLine($"OUT: {Convert.ToBase64String(eventArgs.Buffer)}");
        }

        return Task.CompletedTask;
    }
}