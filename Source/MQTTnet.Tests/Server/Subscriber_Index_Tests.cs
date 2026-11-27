// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server;

// Covers the server's index of subscriber sessions by exact topic filter: which
// sessions a publish visits must follow subscribe, unsubscribe and session replacement.
// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Subscriber_Index_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Exact_And_Wildcard_Filters_Deliver_Once()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        var both = await testEnvironment.ConnectClient(o => o.WithClientId("both"));
        var bothMessages = testEnvironment.CreateApplicationMessageHandler(both);
        await both.SubscribeAsync("a/b");
        await both.SubscribeAsync("a/+");

        var wildcard = await testEnvironment.ConnectClient(o => o.WithClientId("wildcard"));
        var wildcardMessages = testEnvironment.CreateApplicationMessageHandler(wildcard);
        await wildcard.SubscribeAsync("a/#");

        await Inject(server, "a/b");
        await LongTestDelay();

        bothMessages.AssertReceivedCountEquals(1);
        wildcardMessages.AssertReceivedCountEquals(1);
    }

    [TestMethod]
    public async Task Publish_Reaches_Only_Matching_Exact_Subscribers()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        var handlers = new List<TestApplicationMessageReceivedHandler>();
        for (var i = 0; i < 5; i++)
        {
            var client = await testEnvironment.ConnectClient(o => o.WithClientId("device" + i));
            handlers.Add(testEnvironment.CreateApplicationMessageHandler(client));
            await client.SubscribeAsync($"devices/{i}/cmd");
        }

        await Inject(server, "devices/3/cmd");
        await Inject(server, "devices/3/cmd/extra");
        await Inject(server, "devices/3");
        await LongTestDelay();

        for (var i = 0; i < handlers.Count; i++)
        {
            handlers[i].AssertReceivedCountEquals(i == 3 ? 1 : 0);
        }
    }

    [TestMethod]
    public async Task Reconnect_With_Clean_Start_Drops_Old_Subscriptions()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
        var server = await testEnvironment.StartServer(o => o.WithPersistentSessions());

        var client = await testEnvironment.ConnectClient(o => o.WithClientId("device").WithCleanSession(false).WithSessionExpiryInterval(60));
        await client.SubscribeAsync("x");
        await client.DisconnectAsync();
        client.Dispose();

        client = await testEnvironment.ConnectClient(o => o.WithClientId("device").WithCleanSession());
        var messages = testEnvironment.CreateApplicationMessageHandler(client);

        await Inject(server, "x");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(0);

        await client.SubscribeAsync("x");
        await Inject(server, "x");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(1);
    }

    [TestMethod]
    public async Task Reconnect_With_Persistent_Session_Keeps_Subscriptions()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
        var server = await testEnvironment.StartServer(o => o.WithPersistentSessions());

        var client = await testEnvironment.ConnectClient(o => o.WithClientId("device").WithCleanSession(false).WithSessionExpiryInterval(60));
        await client.SubscribeAsync("x");
        await client.DisconnectAsync();
        client.Dispose();

        client = await testEnvironment.ConnectClient(o => o.WithClientId("device").WithCleanSession(false).WithSessionExpiryInterval(60));
        var messages = testEnvironment.CreateApplicationMessageHandler(client);

        await Inject(server, "x");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(1);
    }

    [TestMethod]
    public async Task Resubscribe_After_Session_Deleted_Delivers_Once()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer(o => o.WithPersistentSessions(false));

        var client = await testEnvironment.ConnectClient(o => o.WithClientId("device"));
        await client.SubscribeAsync("x");
        await client.DisconnectAsync();
        client.Dispose();
        await LongTestDelay();

        client = await testEnvironment.ConnectClient(o => o.WithClientId("device"));
        var messages = testEnvironment.CreateApplicationMessageHandler(client);
        await client.SubscribeAsync("x");

        await Inject(server, "x");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(1);
    }

    [TestMethod]
    public async Task Unsubscribe_Exact_Filter_Keeps_Wildcard_Filter()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient();
        var messages = testEnvironment.CreateApplicationMessageHandler(client);
        await client.SubscribeAsync("a/b");
        await client.SubscribeAsync("a/#");
        await client.UnsubscribeAsync("a/b");

        await Inject(server, "a/b");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(1);
    }

    [TestMethod]
    public async Task Unsubscribe_Wildcard_Filter_Keeps_Exact_Filter()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        var client = await testEnvironment.ConnectClient();
        var messages = testEnvironment.CreateApplicationMessageHandler(client);
        await client.SubscribeAsync("a/b");
        await client.SubscribeAsync("a/#");
        await client.UnsubscribeAsync("a/#");

        await Inject(server, "a/c");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(0);

        await Inject(server, "a/b");
        await LongTestDelay();
        messages.AssertReceivedCountEquals(1);
    }

    static Task Inject(MqttServer server, string topic)
    {
        return server.InjectApplicationMessage(new InjectedMqttApplicationMessage(new MqttApplicationMessageBuilder().WithTopic(topic).Build()));
    }
}
