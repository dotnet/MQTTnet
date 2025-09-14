// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.MQTTv5;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[TestClass]
public sealed class Client_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Connect()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();
        await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500).Build());
    }

    [TestMethod]
    public async Task Connect_And_Disconnect()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        await client.DisconnectAsync();
    }

    [TestMethod]
    public async Task Connect_With_New_Mqtt_Features()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        // This test can be also executed against "broker.hivemq.com" to validate package format.
        var client = await testEnvironment.ConnectClient(
            new MqttClientOptionsBuilder()
                //.WithTcpServer("broker.hivemq.com")
                .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithTopicAliasMaximum(20)
                .WithReceiveMaximum(20)
                .WithWillTopic("abc")
                .WithWillDelayInterval(20)
                .Build());

        MqttApplicationMessage receivedMessage = null;

        await client.SubscribeAsync("a");
        client.ApplicationMessageReceivedAsync += e =>
        {
            receivedMessage = e.ApplicationMessage;
            return CompletedTask.Instance;
        };

        await client.PublishAsync(
            new MqttApplicationMessageBuilder().WithTopic("a")
                .WithPayload("x")
                .WithUserProperty("a", "1")
                .WithUserProperty("b", "2")
                .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

        await Task.Delay(500);

        Assert.IsNotNull(receivedMessage);

        Assert.AreEqual(2, receivedMessage.UserProperties.Count);
    }

    [TestMethod]
    public async Task Publish_And_Receive_New_Properties()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var receiver = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
        await receiver.SubscribeAsync("#");

        MqttApplicationMessage receivedMessage = null;
        receiver.ApplicationMessageReceivedAsync += e =>
        {
            receivedMessage = e.ApplicationMessage;
            return CompletedTask.Instance;
        };

        var sender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

        var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("Hello")
            .WithPayload("World")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithUserProperty("x", "1")
            .WithUserProperty("y", "2")
            .WithResponseTopic("response")
            .WithContentType("text")
            .WithMessageExpiryInterval(50)
            .WithCorrelationData(new byte[12])
            .WithTopicAlias(2)
            .Build();

        await sender.PublishAsync(applicationMessage);

        await Task.Delay(500);

        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual(applicationMessage.Topic, receivedMessage.Topic);
        Assert.AreEqual(applicationMessage.TopicAlias, receivedMessage.TopicAlias);
        Assert.AreEqual(applicationMessage.ContentType, receivedMessage.ContentType);
        Assert.AreEqual(applicationMessage.ResponseTopic, receivedMessage.ResponseTopic);
        Assert.AreEqual(applicationMessage.MessageExpiryInterval, receivedMessage.MessageExpiryInterval);
        CollectionAssert.AreEqual(applicationMessage.CorrelationData, receivedMessage.CorrelationData);
        CollectionAssert.AreEqual(applicationMessage.Payload.ToArray(), receivedMessage.Payload.ToArray());
        CollectionAssert.AreEqual(applicationMessage.UserProperties, receivedMessage.UserProperties);
    }

    [TestMethod]
    public async Task Publish_QoS_0()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var result = await client.PublishStringAsync("a", "b");
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_QoS_0_LargeBuffer()
    {
        await using var recyclableMemoryStream = GetLargePayload();
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var result = await client.PublishSequenceAsync("a", recyclableMemoryStream.GetReadOnlySequence());
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_QoS_1()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var result = await client.PublishStringAsync("a", "b", MqttQualityOfServiceLevel.AtLeastOnce);
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.NoMatchingSubscribers, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_QoS_1_LargeBuffer()
    {
        await using var recyclableMemoryStream = GetLargePayload();
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var result = await client.PublishSequenceAsync("a", recyclableMemoryStream.GetReadOnlySequence(), MqttQualityOfServiceLevel.AtLeastOnce);
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.NoMatchingSubscribers, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_QoS_2()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var result = await client.PublishStringAsync("a", "b", MqttQualityOfServiceLevel.ExactlyOnce);
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.NoMatchingSubscribers, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_QoS_2_LargeBuffer()
    {
        await using var recyclableMemoryStream = GetLargePayload();
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var result = await client.PublishSequenceAsync("a", recyclableMemoryStream.GetReadOnlySequence(), MqttQualityOfServiceLevel.ExactlyOnce);
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.NoMatchingSubscribers, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_With_Properties()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));

        var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("Hello")
            .WithPayload("World")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithUserProperty("x", "1")
            .WithUserProperty("y", "2")
            .WithResponseTopic("response")
            .WithContentType("text")
            .WithMessageExpiryInterval(50)
            .WithCorrelationData(new byte[12])
            .WithTopicAlias(2)
            .Build();

        var result = await client.PublishAsync(applicationMessage);
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [TestMethod]
    public async Task Publish_With_RecyclableMemoryStream()
    {
        var memoryManager = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options { BlockSize = 4096 });
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));

        const int payloadSize = 100000;
        await using var memoryStream = memoryManager.GetStream();

        byte testValue = 0;
        while (memoryStream.Length < payloadSize)
        {
            memoryStream.WriteByte(testValue++);
        }

        var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("Hello")
            .WithPayload(memoryStream.GetReadOnlySequence())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithUserProperty("x", "1")
            .WithUserProperty("y", "2")
            .WithResponseTopic("response")
            .WithContentType("text")
            .WithMessageExpiryInterval(50)
            .WithCorrelationData(new byte[12])
            .WithTopicAlias(2)
            .Build();

        var result = await client.PublishAsync(applicationMessage);
        await client.DisconnectAsync();

        Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [TestMethod]
    public async Task Subscribe()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));

        var result = await client.SubscribeAsync(
            new MqttClientSubscribeOptions
            {
                SubscriptionIdentifier = 1,
                TopicFilters =
                [
                    new MqttTopicFilter() { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }
                ]
            });

        await client.DisconnectAsync();

        Assert.AreEqual(1, result.Items.Count);
        Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS1, result.Items.First().ResultCode);
    }

    [TestMethod]
    public async Task Subscribe_And_Publish()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client1 = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500).WithClientId("client1"));

        var testMessageHandler = testEnvironment.CreateApplicationMessageHandler(client1);

        await client1.SubscribeAsync("a");

        var client2 = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500).WithClientId("client2"));
        await client2.PublishStringAsync("a", "b");

        await Task.Delay(500);

        await client2.DisconnectAsync();
        await client1.DisconnectAsync();

        Assert.AreEqual(1, testMessageHandler.ReceivedEventArgs.Count);
        Assert.AreEqual("Subscribe_And_Publish_client1", testMessageHandler.ReceivedEventArgs[0].ClientId);
        Assert.AreEqual("a", testMessageHandler.ReceivedEventArgs[0].ApplicationMessage.Topic);
        Assert.AreEqual("b", testMessageHandler.ReceivedEventArgs[0].ApplicationMessage.ConvertPayloadToString());
    }

    [TestMethod]
    public async Task Unsubscribe()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        await client.SubscribeAsync("a");

        var result = await client.UnsubscribeAsync("a");
        await client.DisconnectAsync();

        Assert.AreEqual(1, result.Items.Count);
        Assert.AreEqual(MqttClientUnsubscribeResultCode.Success, result.Items.First().ResultCode);
    }

    static RecyclableMemoryStream GetLargePayload()
    {
        var memoryManager = new RecyclableMemoryStreamManager();
        var memoryStream = memoryManager.GetStream();
        for (var i = 0; i < 100000; i++)
        {
            memoryStream.WriteByte((byte)i);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }
}