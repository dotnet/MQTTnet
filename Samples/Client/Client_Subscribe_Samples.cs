// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

using MQTTnet.Extensions.TopicTemplate;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Samples.Helpers;

namespace MQTTnet.Samples.Client;

public static class Client_Subscribe_Samples
{
    static readonly MqttTopicTemplate sampleTemplate = new("mqttnet/samples/topic/{id}");

    public static async Task Handle_Received_Application_Message()
    {
        /*
         * This sample subscribes to a topic and processes the received message.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        // Setup message handling before connecting so that queued messages
        // are also handled properly. When there is no event handler attached all
        // received messages get lost.
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            Console.WriteLine("Received application message.");
            e.DumpToConsole();

            return Task.CompletedTask;
        };

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicTemplate(sampleTemplate.WithParameter("id", "2")).Build();

        await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topic.");

        Console.WriteLine("Press enter to exit.");
        Console.ReadLine();
    }

    public static async Task Send_Responses()
    {
        /*
         * This sample subscribes to a topic and sends a response to the broker. This requires at least QoS level 1 to work!
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        mqttClient.ApplicationMessageReceivedAsync += delegate(MqttApplicationMessageReceivedEventArgs args)
        {
            // Do some work with the message...

            // Now respond to the broker with a reason code other than success.
            args.ReasonCode = MqttApplicationMessageReceivedReasonCode.ImplementationSpecificError;
            args.ResponseReasonString = "That did not work!";

            // User properties require MQTT v5!
            args.ResponseUserProperties.Add(new MqttUserProperty("My", "Data"));

            // Now the broker will resend the message again.
            return Task.CompletedTask;
        };

        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicTemplate(sampleTemplate.WithParameter("id", "1")).Build();

        var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topic.");

        // The response contains additional data sent by the server after subscribing.
        response.DumpToConsole();
    }

    public static async Task Subscribe_Multiple_Topics()
    {
        /*
         * This sample subscribes to several topics in a single request.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // Create the subscribe options including several topics with different options.
        // It is also possible to all of these topics using a dedicated call of _SubscribeAsync_ per topic.
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicTemplate(sampleTemplate.WithParameter("id", "1"))
            .WithTopicTemplate(sampleTemplate.WithParameter("id", "2"), noLocal: true)
            .WithTopicTemplate(sampleTemplate.WithParameter("id", "3"), retainHandling: MqttRetainHandling.SendAtSubscribe)
            .Build();

        var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topics.");

        // The response contains additional data sent by the server after subscribing.
        response.DumpToConsole();
    }

    public static async Task Subscribe_Topic()
    {
        /*
         * This sample subscribes to a topic.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicTemplate(sampleTemplate.WithParameter("id", "1")).Build();

        var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topic.");

        // The response contains additional data sent by the server after subscribing.
        response.DumpToConsole();
    }

    static void ConcurrentProcessingDisableAutoAcknowledge(CancellationToken shutdownToken, IMqttClient mqttClient)
    {
        /*
         * This sample shows how to achieve concurrent processing and not have message AutoAcknowledged
         * This to have a proper QoS1 (at-least-once) experience for what at least MQTT specification can provide
         */
        mqttClient.ApplicationMessageReceivedAsync += ea =>
        {
            ea.AutoAcknowledge = false;

            async Task ProcessAsync()
            {
                // DO YOUR WORK HERE!
                await Task.Delay(1000, shutdownToken);
                await ea.AcknowledgeAsync(shutdownToken);
                // WARNING: If process failures are not transient the message will be retried on every restart of the client
                //          A failed message will not be dispatched again to the client as MQTT does not have a NACK packet to let
                //          the broker know processing failed
                //
                // Optionally: Use a framework like Polly to create a retry policy: https://github.com/App-vNext/Polly#retry
            }

            _ = Task.Run(ProcessAsync, shutdownToken);

            return Task.CompletedTask;
        };
    }

    static void ConcurrentProcessingWithLimit(CancellationToken shutdownToken, IMqttClient mqttClient)
    {
        /*
         * This sample shows how to achieve concurrent processing, with:
         * - a maximum concurrency limit based on Environment.ProcessorCount
         */

        var concurrent = new SemaphoreSlim(Environment.ProcessorCount);

        mqttClient.ApplicationMessageReceivedAsync += async ea =>
        {
            await concurrent.WaitAsync(shutdownToken).ConfigureAwait(false);

            async Task ProcessAsync()
            {
                try
                {
                    // DO YOUR WORK HERE!
                    await Task.Delay(1000, shutdownToken);
                }
                finally
                {
                    concurrent.Release();
                }
            }

            _ = Task.Run(ProcessAsync, shutdownToken);
        };
    }
}