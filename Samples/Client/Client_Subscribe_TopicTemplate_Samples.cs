// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Extensions.TopicTemplate;
using MQTTnet.Protocol;
using MQTTnet.Samples.Helpers;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace MQTTnet.Samples.Client;

public static class Client_Subscribe_TopicTemplate_Samples
{
    static readonly MqttTopicTemplate SampleTemplate = new("mqttnet/samples/topic/{id}");

    public static async Task Subscribe_Multiple_Topics_With_Template()
    {
        /*
         * This sample subscribes to several topics in a single request.
         *
         * This sample requires the nuget 'MQTTnet.Extensions.TopicTemplate'.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // Create the subscribe options including several topics with different options.
        // It is also possible to all of these topics using a dedicated call of _SubscribeAsync_ per topic.
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicTemplate(SampleTemplate.WithParameter("id", "1"))
            .WithTopicTemplate(SampleTemplate.WithParameter("id", "2"), noLocal: true)
            .WithTopicTemplate(SampleTemplate.WithParameter("id", "3"), retainHandling: MqttRetainHandling.SendAtSubscribe)
            .Build();

        var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topics.");

        // The response contains additional data sent by the server after subscribing.
        response.DumpToConsole();
    }

    public static async Task Subscribe_Topic_With_Template()
    {
        /*
         * This sample subscribes to a topic.
         *
         * This sample requires the nuget 'MQTTnet.Extensions.TopicTemplate'.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicTemplate(SampleTemplate.WithParameter("id", "1")).Build();

        var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topic.");

        // The response contains additional data sent by the server after subscribing.
        response.DumpToConsole();
    }
}