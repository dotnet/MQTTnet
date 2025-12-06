// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;
using System.Text;

namespace MQTTnet.Samples.Client;

public static class Client_Publish_Samples
{
    public static async Task Publish_Application_Message()
    {
        /*
         * This sample pushes a simple application message including a topic and a payload.
         *
         * Always use builders where they exist. Builders (in this project) are designed to be
         * backward compatible. Creating an _MqttApplicationMessage_ via its constructor is also
         * supported but the class might change often in future releases where the builder does not
         * or at least provides backward compatibility where possible.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.hivemq.com")
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic("samples/temperature/living_room")
            .WithPayload("19.5")
            .Build();

        await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        await mqttClient.DisconnectAsync();

        Console.WriteLine("MQTT application message is published.");
    }

    public static async Task Publish_Multiple_Application_Messages()
    {
        /*
         * This sample pushes multiple simple application message including a topic and a payload.
         *
         * See sample _Publish_Application_Message_ for more details.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.hivemq.com")
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic("samples/temperature/living_room")
            .WithPayload("19.5")
            .Build();

        await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic("samples/temperature/living_room")
            .WithPayload("20.0")
            .Build();

        await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic("samples/temperature/living_room")
            .WithPayload("21.0")
            .Build();

        await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        await mqttClient.DisconnectAsync();

        Console.WriteLine("MQTT application message is published.");
    }

    public static MqttApplicationMessage Create_Message_With_Binary_User_Property()
    {
        /*
         * MQTT v5 user properties are encoded as UTF-8 strings. When the UTF-8 payload is already available
         * as a byte buffer, the builder APIs can avoid creating intermediate strings.
         */

        var encodedValue = Encoding.UTF8.GetBytes("sensor-01");

        return new MqttApplicationMessageBuilder()
            .WithTopic("samples/metadata/binary")
            .WithUserProperty("client-id", encodedValue.AsMemory())
            .WithUserProperty("checksum", new ArraySegment<byte>(encodedValue))
            .WithPayload("metadata")
            .Build();
    }
}