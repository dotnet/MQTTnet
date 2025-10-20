// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Events_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Fire_Client_Connected_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        ClientConnectedEventArgs eventArgs = null;
        server.ClientConnectedAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser", "ThePassword"));

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);

        Assert.StartsWith(nameof(Fire_Client_Connected_Event), eventArgs.ClientId);
        Assert.Contains("127.0.0.1", eventArgs.RemoteEndPoint.ToString()!);
        Assert.AreEqual(MqttProtocolVersion.V311, eventArgs.ProtocolVersion);
        Assert.AreEqual("TheUser", eventArgs.UserName);
        Assert.AreEqual("ThePassword", eventArgs.Password);
    }

    [TestMethod]
    public async Task Fire_Client_Disconnected_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        ClientDisconnectedEventArgs eventArgs = null;
        server.ClientDisconnectedAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser", "ThePassword"));
        await client.DisconnectAsync();

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);

        Assert.StartsWith(nameof(Fire_Client_Disconnected_Event), eventArgs.ClientId);
        Assert.Contains("127.0.0.1", eventArgs.RemoteEndPoint.ToString()!);
        Assert.AreEqual(MqttClientDisconnectType.Clean, eventArgs.DisconnectType);

        Assert.AreEqual("TheUser", eventArgs.UserName);
        Assert.AreEqual("ThePassword", eventArgs.Password);
    }

    [TestMethod]
    public async Task Fire_Client_Subscribed_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        ClientSubscribedTopicEventArgs eventArgs = null;
        server.ClientSubscribedTopicAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
        await client.SubscribeAsync("The/Topic", MqttQualityOfServiceLevel.AtLeastOnce);

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);

        Assert.StartsWith(nameof(Fire_Client_Subscribed_Event), eventArgs.ClientId);
        Assert.AreEqual("The/Topic", eventArgs.TopicFilter.Topic);
        Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, eventArgs.TopicFilter.QualityOfServiceLevel);
        Assert.AreEqual("TheUser", eventArgs.UserName);
    }

    [TestMethod]
    public async Task Fire_Client_Unsubscribed_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        ClientUnsubscribedTopicEventArgs eventArgs = null;
        server.ClientUnsubscribedTopicAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
        await client.UnsubscribeAsync("The/Topic");

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);

        Assert.StartsWith(nameof(Fire_Client_Unsubscribed_Event), eventArgs.ClientId);
        Assert.AreEqual("The/Topic", eventArgs.TopicFilter);
        Assert.AreEqual("TheUser", eventArgs.UserName);
    }

    [TestMethod]
    public async Task Fire_Application_Message_Received_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        InterceptingPublishEventArgs eventArgs = null;
        server.InterceptingPublishAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
        await client.PublishStringAsync("The_Topic", "The_Payload");

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);

        Assert.StartsWith(nameof(Fire_Application_Message_Received_Event), eventArgs.ClientId);
        Assert.AreEqual("The_Topic", eventArgs.ApplicationMessage.Topic);
        Assert.AreEqual("The_Payload", eventArgs.ApplicationMessage.ConvertPayloadToString());
        Assert.AreEqual("TheUser", eventArgs.UserName);
    }

    [TestMethod]
    public async Task Fire_Started_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = testEnvironment.CreateServer(new MqttServerOptions());

        EventArgs eventArgs = null;
        server.StartedAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        await server.StartAsync();

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);
    }

    [TestMethod]
    public async Task Fire_Stopped_Event()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        EventArgs eventArgs = null;
        server.StoppedAsync += e =>
        {
            eventArgs = e;
            return CompletedTask.Instance;
        };

        await server.StopAsync();

        await LongTestDelay();

        Assert.IsNotNull(eventArgs);
    }
}