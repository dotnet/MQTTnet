// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using MQTTnet.Extensions.DelayedPublish;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class DelayedPublish_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Delayed_Publish_Is_Dispatched_After_Interval()
    {
        using var testEnvironment = CreateTestEnvironment();

        var server = await testEnvironment.StartServer();
        await using var _ = server.UseDelayedPublish();

        var subscriber = await testEnvironment.ConnectClient();
        var handler = testEnvironment.CreateApplicationMessageHandler(subscriber);
        await subscriber.SubscribeAsync("sensors/temp");

        var publisher = await testEnvironment.ConnectClient();

        var stopwatch = Stopwatch.StartNew();
        await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("$delayed/1/sensors/temp")
            .WithPayload("23")
            .Build());

        // Should not be delivered immediately.
        await MediumTestDelay();
        Assert.IsEmpty(handler.ReceivedEventArgs);

        // Wait until the delay has clearly elapsed.
        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.HasCount(1, handler.ReceivedEventArgs);
        Assert.AreEqual("sensors/temp", handler.ReceivedEventArgs[0].ApplicationMessage.Topic);
        Assert.IsTrue(stopwatch.Elapsed >= TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task Subscriber_On_Delayed_Prefix_Does_Not_Receive()
    {
        using var testEnvironment = CreateTestEnvironment();

        var server = await testEnvironment.StartServer();
        await using var _ = server.UseDelayedPublish();

        var prefixSubscriber = await testEnvironment.ConnectClient();
        var prefixHandler = testEnvironment.CreateApplicationMessageHandler(prefixSubscriber);
        await prefixSubscriber.SubscribeAsync("$delayed/#");

        var realSubscriber = await testEnvironment.ConnectClient();
        var realHandler = testEnvironment.CreateApplicationMessageHandler(realSubscriber);
        await realSubscriber.SubscribeAsync("rooms/kitchen");

        var publisher = await testEnvironment.ConnectClient();
        await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("$delayed/1/rooms/kitchen")
            .WithPayload("hello")
            .Build());

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.IsEmpty(prefixHandler.ReceivedEventArgs);
        Assert.HasCount(1, realHandler.ReceivedEventArgs);
        Assert.AreEqual("rooms/kitchen", realHandler.ReceivedEventArgs[0].ApplicationMessage.Topic);
    }

    [TestMethod]
    public async Task Retain_Flag_Is_Preserved_At_Dispatch_Time()
    {
        using var testEnvironment = CreateTestEnvironment();

        var server = await testEnvironment.StartServer();
        await using var _ = server.UseDelayedPublish();

        var publisher = await testEnvironment.ConnectClient();
        await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("$delayed/1/status/device-1")
            .WithPayload("online")
            .WithRetainFlag()
            .Build());

        // No retained message yet.
        var retainedBefore = await server.GetRetainedMessagesAsync();
        Assert.IsFalse(retainedBefore.Any(m => m.Topic == "status/device-1"));

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        var retainedAfter = await server.GetRetainedMessagesAsync();
        var retained = retainedAfter.SingleOrDefault(m => m.Topic == "status/device-1");
        Assert.IsNotNull(retained);
        Assert.IsTrue(retained.Retain);
    }

    [TestMethod]
    public async Task Invalid_Interval_Is_Rejected_And_Not_Dispatched()
    {
        using var testEnvironment = CreateTestEnvironment();
        testEnvironment.IgnoreServerLogErrors = true;

        var server = await testEnvironment.StartServer();
        await using var _ = server.UseDelayedPublish();

        var subscriber = await testEnvironment.ConnectClient();
        var handler = testEnvironment.CreateApplicationMessageHandler(subscriber);
        await subscriber.SubscribeAsync("#");

        var publisher = await testEnvironment.ConnectClient();
        await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("$delayed/abc/foo")
            .WithPayload("nope")
            .Build());

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        // The message must not surface on any topic.
        Assert.IsEmpty(handler.ReceivedEventArgs);
    }

    [TestMethod]
    public async Task MaxDelay_Exceeded_Is_Rejected()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);

        var server = await testEnvironment.StartServer();
        await using var _ = server.UseDelayedPublish(new DelayedPublishOptions
        {
            MaxDelay = TimeSpan.FromSeconds(5)
        });

        var publisher = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));

        var result = await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("$delayed/3600/too/long")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        Assert.AreEqual(MqttClientPublishReasonCode.TopicNameInvalid, result.ReasonCode);
    }

    [TestMethod]
    public async Task User_Properties_Are_Preserved()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);

        var server = await testEnvironment.StartServer();
        await using var _ = server.UseDelayedPublish();

        var subscriber = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var handler = testEnvironment.CreateApplicationMessageHandler(subscriber);
        await subscriber.SubscribeAsync("events/signed");

        var publisher = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("$delayed/1/events/signed")
            .WithPayload("payload")
            .WithUserProperty("trace-id", "abc-123"u8.ToArray())
            .WithContentType("application/json")
            .Build());

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.HasCount(1, handler.ReceivedEventArgs);
        var message = handler.ReceivedEventArgs[0].ApplicationMessage;
        Assert.AreEqual("application/json", message.ContentType);
        Assert.IsNotNull(message.UserProperties);
        Assert.IsTrue(message.UserProperties.Any(p => p.Name == "trace-id" && p.ReadValueAsString() == "abc-123"));
    }
}
