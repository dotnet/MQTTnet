// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class WillLifecycle_Tests
{
    [TestMethod]
    [DataRow(MqttClientDisconnectOptionsReason.NormalDisconnection, false)]
    [DataRow(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage, true)]
    [DataRow(MqttClientDisconnectOptionsReason.UnspecifiedError, true)]
    public async Task Disconnect_Reason_Controls_Will(MqttClientDisconnectOptionsReason reason, bool expected)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var will = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        server.InterceptingPublishAsync += _ => { Interlocked.Increment(ref count); will.TrySetResult(); return Task.CompletedTask; };
        var sender = await environment.ConnectClient(Options("sender", 0, 30));
        await sender.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(reason).Build());
        if (expected)
        {
            await will.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }

        await Task.Delay(200);
        Assert.AreEqual(expected ? 1 : 0, count);
    }

    [TestMethod]
    public async Task Abnormal_Disconnect_Observes_Will_Delay()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var will = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.InterceptingPublishAsync += _ => { will.TrySetResult(); return Task.CompletedTask; };
        var sender = await environment.ConnectClient(Options("sender", 1, 30));
        sender.Dispose();
        await Task.Delay(200);
        Assert.IsFalse(will.Task.IsCompleted);
        await will.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(1U)]
    public async Task Session_Expiry_Limits_Will_Delay(uint expiry)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var sender = await environment.ConnectClient(Options("sender", uint.MaxValue, expiry));
        await sender.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage).Build());
        if (expiry > 0)
        {
            await Task.Delay(200);
            Assert.IsEmpty(messages);
        }

        await WaitForCount(messages, 1);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Reconnect_Resumes_Or_Ends_Pending_Will(bool cleanStart)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var disconnected = Disconnected(server);
        var sender = await environment.ConnectClient(Options("sender", 1, 30));
        sender.Dispose();
        await disconnected.WaitAsync(TimeSpan.FromSeconds(3));
        var resumed = await environment.ConnectClient(NoWill("sender", cleanStart));
        await Task.Delay(1300);
        Assert.HasCount(cleanStart ? 1 : 0, messages);
        await resumed.DisconnectAsync();
    }

    [TestMethod]
    [DataRow(0U, false, true)]
    [DataRow(1U, false, false)]
    [DataRow(1U, true, true)]
    public async Task Takeover_Uses_Old_Connection_Will(uint delay, bool cleanStart, bool expectWill)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        await environment.ConnectClient(Options("sender", delay, 30).WithCredentials("old-user"));
        var resumed = await environment.ConnectClient(NoWill("sender", cleanStart).WithCredentials("new-user"));
        await Task.Delay(1300);
        Assert.HasCount(expectWill ? 1 : 0, messages);
        if (expectWill)
        {
            Assert.AreEqual("will/sender", messages.Single().ApplicationMessage.Topic);
            Assert.AreEqual("old-user", messages.Single().UserName);
        }

        await resumed.DisconnectAsync();
    }

    [TestMethod]
    public async Task Reconnect_After_Expiry_Does_Not_Cancel_Will()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var disconnected = Disconnected(server);
        var sender = await environment.ConnectClient(Options("sender", 30, 1));
        sender.Dispose();
        await disconnected.WaitAsync(TimeSpan.FromSeconds(3));
        await Task.Delay(1200);
        var resumed = await environment.ConnectClient(NoWill("sender", false));
        await WaitForCount(messages, 1);
        await resumed.DisconnectAsync();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1000)]
    public async Task Session_Deletion_And_Timer_Publish_Only_Once(int deletionDelay)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var disconnected = Disconnected(server);
        var sender = await environment.ConnectClient(Options("sender", 1, 30));
        sender.Dispose();
        await disconnected.WaitAsync(TimeSpan.FromSeconds(3));
        var session = (await server.GetSessionsAsync()).Single();
        await Task.Delay(deletionDelay);
        await session.DeleteAsync();
        await WaitForCount(messages, 1);
        await Task.Delay(300);
        Assert.HasCount(1, messages);
    }

    [TestMethod]
    public async Task Disabled_Persistence_Ends_Will_Delay()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer();
        var messages = Capture(server);
        var sender = await environment.ConnectClient(Options("sender", uint.MaxValue, 30));
        sender.Dispose();
        await WaitForCount(messages, 1);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Stop_Restart_Preserves_Pending_Deadline(bool disconnectFirst)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var disconnected = Disconnected(server);
        var sender = await environment.ConnectClient(Options("sender", 1, 30));
        if (disconnectFirst)
        {
            sender.Dispose();
            await disconnected.WaitAsync(TimeSpan.FromSeconds(3));
        }

        await server.StopAsync();
        await Task.Delay(1200);
        Assert.IsEmpty(messages);
        await server.StartAsync();
        await WaitForCount(messages, 1);
    }

    [TestMethod]
    public async Task Rejected_Reconnect_Does_Not_Cancel_Pending_Will()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var disconnected = Disconnected(server);
        var sender = await environment.ConnectClient(Options("sender", 1, 30));
        sender.Dispose();
        await disconnected.WaitAsync(TimeSpan.FromSeconds(3));
        server.ValidatingConnectionAsync += args => { args.ReasonCode = MqttConnectReasonCode.NotAuthorized; return Task.CompletedTask; };
        var rejected = environment.CreateClient();
        var result = await rejected.ConnectAsync(NoWill("sender", false).WithTcpServer("127.0.0.1", environment.ServerPort).Build());
        Assert.AreEqual(MqttClientConnectResultCode.NotAuthorized, result.ResultCode);
        await WaitForCount(messages, 1);
    }

    [TestMethod]
    public async Task Resumed_Connection_Owns_Its_New_Will()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        await environment.ConnectClient(Options("sender", 1, 30));
        var resumed = await environment.ConnectClient(Options("sender", 0, 30, false).WithWillTopic("will/new"));
        await resumed.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage).Build());
        await WaitForCount(messages, 1);
        await Task.Delay(1200);
        Assert.HasCount(1, messages);
        Assert.AreEqual("will/new", messages.Single().ApplicationMessage.Topic);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Will_Callback_Can_Stop_Server_Without_Losing_The_Next_Will(bool duringRestart)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var first = await environment.ConnectClient(Options("first", 1, 30));
        var second = await environment.ConnectClient(Options("second", 1, 30));
        await first.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage).Build());
        await second.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage).Build());
        if (duringRestart)
        {
            await server.StopAsync();
            await Task.Delay(1200);
        }

        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopOnce = 1;
        server.InterceptingPublishAsync += async _ =>
        {
            if (Interlocked.Exchange(ref stopOnce, 0) == 1)
            {
                await server.StopAsync();
                stopped.TrySetResult();
            }
        };
        if (duringRestart)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() => server.StartAsync().WaitAsync(TimeSpan.FromSeconds(3)));
        }

        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await Task.Delay(200);
        Assert.HasCount(1, messages);
        await server.StartAsync();
        await WaitForCount(messages, 2);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Nonpersistent_Shutdown_Does_Not_Recursively_Invoke_Will_Callbacks(bool externalStop)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer();
        var messages = Capture(server);
        var first = await environment.ConnectClient(Options("first", 0, 30));
        await environment.ConnectClient(Options("second", uint.MaxValue, 30));
        var depth = 0;
        var maxDepth = 0;
        server.InterceptingPublishAsync += async _ =>
        {
            depth++;
            maxDepth = Math.Max(maxDepth, depth);
            await server.StopAsync();
            depth--;
        };
        if (externalStop)
        {
            await server.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
        }
        else
        {
            // The first Will stops the server while the second connection is still active.
            first.Dispose();
        }

        await WaitForCount(messages, 2);
        Assert.AreEqual(1, maxDepth);
    }

    [TestMethod]
    public async Task Final_Disposal_Publishes_Pending_Will()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        await environment.ConnectClient(Options("sender", uint.MaxValue, 30));
        server.Dispose();
        Assert.HasCount(1, messages);
    }

    [TestMethod]
    public async Task Delayed_Will_Retains_Message_Expiry_And_Retain_Flag()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var messages = Capture(server);
        var sender = await environment.ConnectClient(Options("sender", 1, 30).WithWillRetain().WithWillMessageExpiryInterval(10));
        sender.Dispose();
        await WaitForCount(messages, 1);
        Assert.AreEqual(10U, messages.Single().ApplicationMessage.MessageExpiryInterval);
        Assert.IsTrue(messages.Single().ApplicationMessage.Retain);
        Assert.HasCount(1, await server.GetRetainedMessagesAsync());
    }

    static ConcurrentQueue<InterceptingPublishEventArgs> Capture(MqttServer server)
    {
        var messages = new ConcurrentQueue<InterceptingPublishEventArgs>();
        server.InterceptingPublishAsync += args => { messages.Enqueue(args); return Task.CompletedTask; };
        return messages;
    }

    static Task Disconnected(MqttServer server)
    {
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientDisconnectedAsync += _ => { disconnected.TrySetResult(); return Task.CompletedTask; };
        return disconnected.Task;
    }

    static async Task WaitForCount(ConcurrentQueue<InterceptingPublishEventArgs> messages, int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (messages.Count < count)
        {
            await Task.Delay(20, timeout.Token);
        }

        Assert.HasCount(count, messages);
    }

    static MqttClientOptionsBuilder NoWill(string id, bool clean)
    {
        return new MqttClientOptionsBuilder().WithClientId(id).WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanSession(clean).WithSessionExpiryInterval(30);
    }

    static MqttClientOptionsBuilder Options(string id, uint delay, uint expiry, bool clean = true)
    {
        return new MqttClientOptionsBuilder().WithClientId(id).WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanSession(clean).WithSessionExpiryInterval(expiry).WithWillTopic("will/" + id)
            .WithWillPayload("synthetic").WithWillDelayInterval(delay);
    }
}