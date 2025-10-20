// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Helpers;

namespace MQTTnet.Tests.Server;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Subscribe_Tests : BaseTestClass
{
    [TestMethod]
    [DataRow("A", "A", true)]
    [DataRow("A", "B", false)]
    [DataRow("A", "#", true)]
    [DataRow("A", "+", true)]
    [DataRow("A/B", "A/B", true)]
    [DataRow("A/B", "A/+", true)]
    [DataRow("A/B", "A/#", true)]
    [DataRow("A/B/C", "A/B/C", true)]
    [DataRow("A/B/C", "A/+/C", true)]
    [DataRow("A/B/C", "A/+/+", true)]
    [DataRow("A/B/C", "A/+/#", true)]
    [DataRow("A/B/C/D", "A/B/C/D", true)]
    [DataRow("A/B/C/D", "A/+/C/+", true)]
    [DataRow("A/B/C/D", "A/+/C/#", true)]
    [DataRow("A/B/C", "A/B/+", true)]
    [DataRow("A/B1/B2/C", "A/+/C", false)]
    public async Task Subscription_Roundtrip(string topic, string filter, bool shouldWork)
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var receiver = await testEnvironment.ConnectClient();
        await receiver.SubscribeAsync(filter);
        var receivedMessages = receiver.TrackReceivedMessages();

        var sender = await testEnvironment.ConnectClient();
        await sender.PublishStringAsync(topic, "PAYLOAD");

        await LongTestDelay();

        if (shouldWork)
        {
            Assert.AreEqual(1, receivedMessages.Count, message: "The filter should work!");
        }
        else
        {
            Assert.AreEqual(0, receivedMessages.Count, message: "The filter should not work!");
        }
    }

    [TestMethod]
    public async Task Deny_Invalid_Topic()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
        var server = await testEnvironment.StartServer();

        server.InterceptingSubscriptionAsync += e =>
        {
            if (e.TopicFilter.Topic == "not_allowed_topic")
            {
                e.Response.ReasonCode = MqttSubscribeReasonCode.TopicFilterInvalid;
            }

            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient();

        var subscribeResult = await client.SubscribeAsync("allowed_topic");
        Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS0, subscribeResult.Items.First().ResultCode);

        subscribeResult = await client.SubscribeAsync("not_allowed_topic");
        Assert.AreEqual(MqttClientSubscribeResultCode.TopicFilterInvalid, subscribeResult.Items.First().ResultCode);
    }

    [TestMethod]
    public async Task Intercept_Subscribe_With_User_Properties()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
        var server = await testEnvironment.StartServer();

        InterceptingSubscriptionEventArgs eventArgs = null;
        server.InterceptingSubscriptionAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient();

        var subscribeOptions = testEnvironment.ClientFactory.CreateSubscribeOptionsBuilder().WithTopicFilter("X").WithUserProperty("A", "1").Build();
        await client.SubscribeAsync(subscribeOptions);

        CollectionAssert.AreEqual(subscribeOptions.UserProperties.ToList(), eventArgs.UserProperties);
    }

    [TestMethod]
    public Task Disconnect_While_Subscribing()
    {
        return Assert.ThrowsExactlyAsync<MqttClientDisconnectedException>(async () =>
        {
            using var testEnvironment = CreateTestEnvironment();
            var server = await testEnvironment.StartServer();

            // The client will be disconnected directly after subscribing!
            server.ClientSubscribedTopicAsync += ev => server.DisconnectClientAsync(ev.ClientId);

            var client = await testEnvironment.ConnectClient();
            await client.SubscribeAsync("#");
        });
    }

    [TestMethod]
    public async Task Enqueue_Message_After_Subscription()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        server.ClientSubscribedTopicAsync += _ =>
        {
            server.InjectApplicationMessage(new InjectedMqttApplicationMessage(new MqttApplicationMessageBuilder().WithTopic("test_topic").Build()));
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient();
        var receivedMessages = testEnvironment.CreateApplicationMessageHandler(client);

        await client.SubscribeAsync("test_topic");

        await LongTestDelay();

        Assert.HasCount(1, receivedMessages.ReceivedEventArgs);
    }

    [TestMethod]
    public async Task Intercept_Subscription()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        server.InterceptingSubscriptionAsync += e =>
        {
            // Set the topic to "a" regards what the client wants to subscribe.
            e.TopicFilter.Topic = "a";
            return CompletedTask.Instance;
        };

        var topicAReceived = false;
        var topicBReceived = false;

        var client = await testEnvironment.ConnectClient();
        client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == "a")
            {
                topicAReceived = true;
            }
            else if (e.ApplicationMessage.Topic == "b")
            {
                topicBReceived = true;
            }

            return CompletedTask.Instance;
        };

        await client.SubscribeAsync("b");

        await client.PublishStringAsync("a");

        await Task.Delay(500);

        Assert.IsTrue(topicAReceived);
        Assert.IsFalse(topicBReceived);
    }

    [TestMethod]
    public async Task Response_Contains_Equal_Reason_Codes()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();
        var client = await testEnvironment.ConnectClient();

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter("a")
            .WithTopicFilter("b", MqttQualityOfServiceLevel.AtLeastOnce)
            .WithTopicFilter("c", MqttQualityOfServiceLevel.ExactlyOnce)
            .WithTopicFilter("d")
            .Build();

        var response = await client.SubscribeAsync(subscribeOptions);

        Assert.HasCount(subscribeOptions.TopicFilters.Count, response.Items);
    }

    [TestMethod]
    public async Task Subscribe_Lots_In_Multiple_Requests()
    {
        using var testEnvironment = CreateTestEnvironment();
        var receivedMessagesCount = 0;

        await testEnvironment.StartServer();

        var c1 = await testEnvironment.ConnectClient();
        c1.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessagesCount);
            return CompletedTask.Instance;
        };

        for (var i = 0; i < 500; i++)
        {
            var so = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(i.ToString()).Build();

            await c1.SubscribeAsync(so);

            await Task.Delay(10);
        }

        var c2 = await testEnvironment.ConnectClient();

        var messageBuilder = new MqttApplicationMessageBuilder();
        for (var i = 0; i < 500; i++)
        {
            messageBuilder.WithTopic(i.ToString());

            await c2.PublishAsync(messageBuilder.Build());

            await Task.Delay(10);
        }

        SpinWait.SpinUntil(() => receivedMessagesCount == 500, 5000);

        Assert.AreEqual(500, receivedMessagesCount);
    }

    [TestMethod]
    public async Task Subscribe_Lots_In_Single_Request()
    {
        using var testEnvironment = CreateTestEnvironment();
        var receivedMessagesCount = 0;

        await testEnvironment.StartServer();

        var c1 = await testEnvironment.ConnectClient();
        c1.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessagesCount);
            return CompletedTask.Instance;
        };

        var optionsBuilder = new MqttClientSubscribeOptionsBuilder();
        for (var i = 0; i < 500; i++)
        {
            optionsBuilder.WithTopicFilter(i.ToString());
        }

        await c1.SubscribeAsync(optionsBuilder.Build());

        var c2 = await testEnvironment.ConnectClient();

        var messageBuilder = new MqttApplicationMessageBuilder();
        for (var i = 0; i < 500; i++)
        {
            messageBuilder.WithTopic(i.ToString());

            await c2.PublishAsync(messageBuilder.Build());
        }

        SpinWait.SpinUntil(() => receivedMessagesCount == 500, TimeSpan.FromSeconds(20));

        Assert.AreEqual(500, receivedMessagesCount);
    }

    [TestMethod]
    public async Task Subscribe_Multiple_In_Multiple_Request()
    {
        using var testEnvironment = CreateTestEnvironment();
        var receivedMessagesCount = 0;

        await testEnvironment.StartServer();

        var c1 = await testEnvironment.ConnectClient();
        c1.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessagesCount);
            return CompletedTask.Instance;
        };

        await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("a").Build());

        await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("b").Build());

        await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("c").Build());

        var c2 = await testEnvironment.ConnectClient();

        await c2.PublishStringAsync("a");
        await Task.Delay(100);
        Assert.AreEqual(1, receivedMessagesCount);

        await c2.PublishStringAsync("b");
        await Task.Delay(100);
        Assert.AreEqual(2, receivedMessagesCount);

        await c2.PublishStringAsync("c");
        await Task.Delay(100);
        Assert.AreEqual(3, receivedMessagesCount);
    }

    [TestMethod]
    public async Task Subscribe_Multiple_In_Single_Request()
    {
        using var testEnvironment = CreateTestEnvironment();
        var receivedMessagesCount = 0;

        await testEnvironment.StartServer();

        var c1 = await testEnvironment.ConnectClient();
        c1.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessagesCount);
            return CompletedTask.Instance;
        };

        await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("a").WithTopicFilter("b").WithTopicFilter("c").Build());

        var c2 = await testEnvironment.ConnectClient();

        await c2.PublishStringAsync("a");
        await Task.Delay(100);
        Assert.AreEqual(1, receivedMessagesCount);

        await c2.PublishStringAsync("b");
        await Task.Delay(100);
        Assert.AreEqual(2, receivedMessagesCount);

        await c2.PublishStringAsync("c");
        await Task.Delay(100);
        Assert.AreEqual(3, receivedMessagesCount);
    }

    [TestMethod]
    public async Task Subscribe_Unsubscribe()
    {
        using var testEnvironment = CreateTestEnvironment();
        var receivedMessagesCount = 0;

        var server = await testEnvironment.StartServer();

        var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("c1"));
        c1.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessagesCount);
            return CompletedTask.Instance;
        };

        var c2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("c2"));

        var message = new MqttApplicationMessageBuilder().WithTopic("a").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build();
        await c2.PublishAsync(message);

        await Task.Delay(500);
        Assert.AreEqual(0, receivedMessagesCount);

        var subscribeEventCalled = false;
        server.ClientSubscribedTopicAsync += e =>
        {
            subscribeEventCalled = e.TopicFilter.Topic == "a" && e.ClientId == c1.Options.ClientId;
            return CompletedTask.Instance;
        };

        await c1.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
        await Task.Delay(250);
        Assert.IsTrue(subscribeEventCalled, "Subscribe event not called.");

        await c2.PublishAsync(message);
        await Task.Delay(250);
        Assert.AreEqual(1, receivedMessagesCount);

        var unsubscribeEventCalled = false;
        server.ClientUnsubscribedTopicAsync += e =>
        {
            unsubscribeEventCalled = e.TopicFilter == "a" && e.ClientId == c1.Options.ClientId;
            return CompletedTask.Instance;
        };

        await c1.UnsubscribeAsync("a");
        await Task.Delay(250);
        Assert.IsTrue(unsubscribeEventCalled, "Unsubscribe event not called.");

        await c2.PublishAsync(message);
        await Task.Delay(500);
        Assert.AreEqual(1, receivedMessagesCount);

        await Task.Delay(500);

        Assert.AreEqual(1, receivedMessagesCount);
    }
}