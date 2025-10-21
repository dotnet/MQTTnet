// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.MQTTv5;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Server_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Will_Message_Send()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will").WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce).WithProtocolVersion(MqttProtocolVersion.V500);

        var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

        var receivedMessagesCount = 0;
        c1.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessagesCount);
            return CompletedTask.Instance;
        };

        await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

        var c2 = await testEnvironment.ConnectClient(clientOptions);
        c2.Dispose(); // Dispose will not send a DISCONNECT packet first so the will message must be sent.

        await LongTestDelay();

        Assert.AreEqual(1, receivedMessagesCount);
    }

    [TestMethod]
    public async Task Validate_IsSessionPresent()
    {
        using var testEnvironment = CreateTestEnvironment();
        // Create server with persistent sessions enabled

        await testEnvironment.StartServer(o => o.WithPersistentSessions());

        const string clientId = "Client1";

        // Create client without clean session and long session expiry interval
        var options1 = CreateClientOptions(testEnvironment, clientId, false, 9999);
        var client1 = await testEnvironment.ConnectClient(options1);

        // Disconnect; empty session should remain on server

        await client1.DisconnectAsync();

        // Simulate some time delay between connections

        await Task.Delay(1000);

        // Reconnect the same client ID to existing session

        var client2 = testEnvironment.CreateClient();
        var options2 = CreateClientOptions(testEnvironment, clientId, false, 9999);
        var result = await client2.ConnectAsync(options2).ConfigureAwait(false);

        await client2.DisconnectAsync();

        // Session should be present

        Assert.IsTrue(result.IsSessionPresent, "Session not present");
    }

    [TestMethod]
    public async Task Connect_with_Undefined_SessionExpiryInterval()
    {
        using var testEnvironment = CreateTestEnvironment();
        // Create server with persistent sessions enabled

        await testEnvironment.StartServer(o => o.WithPersistentSessions());

        const string clientId = "Client1";

        // Create client without clean session and NO session expiry interval,
        // that means, the session should not persist

        var options1 = CreateClientOptions(testEnvironment, clientId, false, 0);
        var client1 = await testEnvironment.ConnectClient(options1);

        // Disconnect; no session should remain on server because the session expiry interval was undefined

        await client1.DisconnectAsync();

        // Simulate some time delay between connections

        await Task.Delay(1000);

        // Reconnect the same client ID to existing session

        var client2 = testEnvironment.CreateClient();
        var options2 = CreateClientOptions(testEnvironment, clientId, false, 9999);
        var result = await client2.ConnectAsync(options2).ConfigureAwait(false);

        await client2.DisconnectAsync();

        // Session should not be present

        Assert.IsFalse(result.IsSessionPresent, "Session is present when it should not");
    }

    [TestMethod]
    public async Task Reconnect_with_different_SessionExpiryInterval()
    {
        using var testEnvironment = CreateTestEnvironment();
        // Create server with persistent sessions enabled

        await testEnvironment.StartServer(o => o.WithPersistentSessions());

        const string clientId = "Client1";

        // Create client with clean session and session expiry interval > 0

        var options = CreateClientOptions(testEnvironment, clientId, true, 9999);
        var client1 = await testEnvironment.ConnectClient(options);

        // Disconnect; session should remain on server

        await client1.DisconnectAsync();

        await Task.Delay(1000);

        // Reconnect the same client ID to the existing session but leave session expiry interval undefined this time.
        // Session should be present because the client1 connection had SessionExpiryInterval > 0

        var client2 = testEnvironment.CreateClient();
        var options2 = CreateClientOptions(testEnvironment, clientId, false, 0);
        var result2 = await client2.ConnectAsync(options2).ConfigureAwait(false);

        await client2.DisconnectAsync();

        Assert.IsTrue(result2.IsSessionPresent, "Session is not present when it should");

        // Simulate some time delay between connections

        await Task.Delay(1000);

        // Reconnect the same client ID.
        // No session should be present because the previous session expiry interval was undefined for the client2 connection

        var client3 = testEnvironment.CreateClient();
        var options3 = CreateClientOptions(testEnvironment, clientId, false, 0);
        var result3 = await client2.ConnectAsync(options3).ConfigureAwait(false);

        await client3.DisconnectAsync();

        // Session should be present

        Assert.IsFalse(result3.IsSessionPresent, "Session is present when it should not");
    }

    [TestMethod]
    public async Task Disconnect_with_Reason()
    {
        using var testEnvironment = CreateTestEnvironment();
        var disconnectReason = MqttClientDisconnectReason.UnspecifiedError;

        string testClientId = null;

        await testEnvironment.StartServer();

        testEnvironment.Server.ClientConnectedAsync += e =>
        {
            testClientId = e.ClientId;
            return CompletedTask.Instance;
        };

        var client = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

        client.DisconnectedAsync += e =>
        {
            disconnectReason = e.Reason;
            return CompletedTask.Instance;
        };

        await LongTestDelay();

        // Test client should be connected now

        Assert.IsNotNull(testClientId);

        // Have the server disconnect the client with AdministrativeAction reason

        await testEnvironment.Server.DisconnectClientAsync(testClientId, MqttDisconnectReasonCode.AdministrativeAction);

        await LongTestDelay();

        // The reason should be returned to the client in the DISCONNECT packet

        Assert.AreEqual(MqttClientDisconnectReason.AdministrativeAction, disconnectReason);
    }

    static MqttClientOptions CreateClientOptions(TestEnvironment testEnvironment, string clientId, bool cleanSession, uint sessionExpiryInterval)
    {
        return testEnvironment.ClientFactory.CreateClientOptionsBuilder()
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
            .WithSessionExpiryInterval(sessionExpiryInterval)
            .WithCleanSession(cleanSession)
            .WithClientId(clientId)
            .Build();
    }
}