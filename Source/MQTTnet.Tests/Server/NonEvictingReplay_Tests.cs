// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class NonEvictingReplay_Tests : BaseTestClass
{
    [TestMethod]
    [DataRow(MqttProtocolVersion.V311, MqttQualityOfServiceLevel.AtMostOnce)]
    [DataRow(MqttProtocolVersion.V311, MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttProtocolVersion.V311, MqttQualityOfServiceLevel.ExactlyOnce)]
    [DataRow(MqttProtocolVersion.V500, MqttQualityOfServiceLevel.AtMostOnce)]
    [DataRow(MqttProtocolVersion.V500, MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttProtocolVersion.V500, MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Retry_After_Connected_Callback_Preserves_Every_Message(MqttProtocolVersion protocol, MqttQualityOfServiceLevel qos)
    {
        const int capacity = 1000;
        const int total = 1156;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var environment = CreateTestEnvironment();
        var server = await environment.StartServer(builder => builder.WithMaxPendingMessagesPerClient(capacity));
        var messages = Enumerable.Range(0, total).Select(id => new MqttApplicationMessageBuilder()
            .WithTopic("bounded-replay/" + id).WithQualityOfServiceLevel(qos).Build()).ToArray();
        var initial = new TaskCompletionSource<MqttSessionStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var overwritten = 0;
        server.QueuedApplicationMessageOverwrittenAsync += _ => { Interlocked.Increment(ref overwritten); return Task.CompletedTask; };
        server.ClientConnectedAsync += async args =>
        {
            var session = (await server.GetSessionsAsync()).Single(s => s.Id == args.ClientId);
            for (var id = 0; id < total; id++)
            {
                var accepted = session.TryEnqueueApplicationMessage(messages[id], out var result, false);
                Assert.AreEqual(id < capacity, accepted);
                if (!accepted) Assert.IsNull(result);
            }

            Assert.AreEqual(capacity, (int)session.PendingApplicationMessagesCount);
            initial.TrySetResult(session);
            // Do not await delivery or a retry pump here: the send loop starts after this callback returns.
        };

        var receiver = environment.CreateClient();
        var seen = new int[total];
        var received = 0;
        var duplicates = 0;
        var invalid = 0;
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ApplicationMessageReceivedAsync += args =>
        {
            var topic = args.ApplicationMessage.Topic;
            if (!topic.StartsWith("bounded-replay/", StringComparison.Ordinal) || !int.TryParse(topic.AsSpan(15), out var id) || id < 0 || id >= total ||
                args.ApplicationMessage.QualityOfServiceLevel != qos)
            {
                Interlocked.Increment(ref invalid);
            }
            else if (Interlocked.Exchange(ref seen[id], 1) != 0)
            {
                Interlocked.Increment(ref duplicates);
            }
            else if (Interlocked.Increment(ref received) == total)
            {
                delivered.TrySetResult();
            }

            return Task.CompletedTask;
        };

        await receiver.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", environment.ServerPort)
            .WithProtocolVersion(protocol).WithClientId("bounded-replay").Build(), timeout.Token);
        var status = await initial.Task.WaitAsync(timeout.Token);
        // This owned retry operation is outside ClientConnected and retains every rejected item.
        for (var id = capacity; id < total; id++)
        {
            while (!status.TryEnqueueApplicationMessage(messages[id], out _, false))
            {
                await Task.Delay(1, timeout.Token);
            }
        }

        await delivered.Task.WaitAsync(timeout.Token);
        await Task.Delay(100, timeout.Token);
        Assert.AreEqual(total, received);
        Assert.AreEqual(0, duplicates);
        Assert.AreEqual(0, invalid);
        Assert.AreEqual(0, overwritten);
        await receiver.DisconnectAsync();
    }
}
