// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Exceptions;
using MQTTnet.LowLevelClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class ReceiveMaximum_Tests
{
    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Publish_Waits_For_Final_Acknowledgement(MqttQualityOfServiceLevel qos)
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", qos);
        await Inject(environment, "flow", qos);
        await Inject(environment, "flow", qos);
        var first = await Receive<MqttPublishPacket>(client);

        // PINGRESP must make progress, and no second PUBLISH may precede it.
        await client.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(client);
        if (qos == MqttQualityOfServiceLevel.ExactlyOnce)
        {
            await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = first.PacketIdentifier });
            var pubRel = await Receive<MqttPubRelPacket>(client);
            Assert.AreEqual(first.PacketIdentifier, pubRel.PacketIdentifier);
            await client.SendAsync(MqttPingReqPacket.Instance);
            await Receive<MqttPingRespPacket>(client);
            await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier });
        }
        else
        {
            await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        }

        var second = await Receive<MqttPublishPacket>(client);
        Assert.AreNotEqual(first.PacketIdentifier, second.PacketIdentifier);
    }

    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Error_Acknowledgement_Releases_Quota(MqttQualityOfServiceLevel qos)
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", qos);
        await Inject(environment, "flow", qos);
        var first = await Receive<MqttPublishPacket>(client);
        await Inject(environment, "flow", qos);
        await client.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(client);
        if (qos == MqttQualityOfServiceLevel.ExactlyOnce)
        {
            await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = first.PacketIdentifier, ReasonCode = MqttPubRecReasonCode.UnspecifiedError });
        }
        else
        {
            await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier, ReasonCode = MqttPubAckReasonCode.UnspecifiedError });
        }

        // An error PUBREC terminates the exchange; it must not produce PUBREL.
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task Receive_Maximum_Is_Per_Connection_And_Capped()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var slow = await Connect(environment, "slow");
        var fast = await Connect(environment, "fast", 2);
        await Subscribe(slow, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Subscribe(fast, "flow", MqttQualityOfServiceLevel.AtLeastOnce);

        // Surplus acknowledgements cannot raise the initial quota.
        await slow.SendAsync(new MqttPubAckPacket { PacketIdentifier = 123 });
        await slow.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(slow);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        var first = await Receive<MqttPublishPacket>(slow);
        await Receive<MqttPublishPacket>(fast);
        await Receive<MqttPublishPacket>(fast);
        await slow.SendAsync(MqttPingReqPacket.Instance);
        await fast.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(slow);
        await Receive<MqttPingRespPacket>(fast);
        await slow.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        await Receive<MqttPublishPacket>(slow);
    }

    [TestMethod]
    public async Task Unknown_And_Duplicate_PubAck_Do_Not_Release_Quota()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        var first = await Receive<MqttPublishPacket>(client);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);

        await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = ushort.MaxValue });
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier });
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        var second = await Receive<MqttPublishPacket>(client);

        await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = second.PacketIdentifier });
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task Unknown_And_Duplicate_Qos2_Acknowledgements_Do_Not_Release_Quota()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        var first = await Receive<MqttPublishPacket>(client);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);

        await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = ushort.MaxValue, ReasonCode = MqttPubRecReasonCode.UnspecifiedError });
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier });
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = first.PacketIdentifier });
        await Receive<MqttPubRelPacket>(client);
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = ushort.MaxValue });
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier });
        var second = await Receive<MqttPublishPacket>(client);

        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier });
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        await AssertPublishRemainsBlocked(environment, client);
        await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = second.PacketIdentifier });
        await Receive<MqttPubRelPacket>(client);
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = second.PacketIdentifier });
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task PubComp_Does_Not_Release_Quota_Until_PubRel_Is_Sent()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer();
        var pubRelIntercepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePubRel = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prematurePubCompIntercepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePrematurePubComp = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interceptPrematurePubComp = true;
        server.InterceptingOutboundPacketAsync += async args =>
        {
            if (args.Packet is MqttPubRelPacket)
            {
                pubRelIntercepted.TrySetResult();
                await releasePubRel.Task;
            }
        };
        server.InterceptingInboundPacketAsync += async args =>
        {
            if (args.Packet is MqttPubCompPacket && interceptPrematurePubComp)
            {
                interceptPrematurePubComp = false;
                prematurePubCompIntercepted.TrySetResult();
                await releasePrematurePubComp.Task;
            }
        };
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        var first = await Receive<MqttPublishPacket>(client);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);

        await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = first.PacketIdentifier });
        await pubRelIntercepted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier });
        await prematurePubCompIntercepted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        releasePrematurePubComp.TrySetResult();
        Assert.AreEqual(1L, (await server.GetSessionsAsync()).Single().PendingApplicationMessagesCount);

        releasePubRel.TrySetResult();
        await Receive<MqttPubRelPacket>(client);
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task Control_Responses_Progress_At_Zero_Quota()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Receive<MqttPublishPacket>(client);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await client.SendAsync(new MqttPublishPacket { Topic = "incoming", PacketIdentifier = 12, QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
        Assert.AreEqual((ushort)12, (await Receive<MqttPubAckPacket>(client)).PacketIdentifier);
        await Subscribe(client, "other", MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [TestMethod]
    public async Task Retained_Messages_Use_Quota()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        await Inject(environment, "retained/1", MqttQualityOfServiceLevel.AtLeastOnce, true);
        await Inject(environment, "retained/2", MqttQualityOfServiceLevel.AtLeastOnce, true);
        var client = await Connect(environment, "receiver");
        await client.SendAsync(new MqttSubscribePacket
        {
            PacketIdentifier = 1,
            TopicFilters = [new MqttTopicFilter { Topic = "retained/#", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }]
        });
        // MQTT permits the retained PUBLISH to precede SUBACK.
        var response = await Receive<MqttPacket>(client);
        MqttPublishPacket first;
        if (response is MqttPublishPacket publish)
        {
            first = publish;
            await Receive<MqttSubAckPacket>(client);
        }
        else
        {
            Assert.IsInstanceOfType<MqttSubAckPacket>(response);
            first = await Receive<MqttPublishPacket>(client);
        }

        await client.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(client);
        await client.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task Reconnect_Resets_Quota_And_Drains_Offline_Messages()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientDisconnectedAsync += _ => { disconnected.TrySetResult(); return Task.CompletedTask; };
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Receive<MqttPublishPacket>(client);
        await client.SendAsync(new MqttDisconnectPacket());
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        var resumed = await Connect(environment, "receiver", clean: false);
        var first = await Receive<MqttPublishPacket>(resumed);
        await resumed.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(resumed);
        await resumed.SendAsync(new MqttPubAckPacket { PacketIdentifier = first.PacketIdentifier });
        await Receive<MqttPublishPacket>(resumed);
    }

    [TestMethod]
    public async Task Blocked_Publishes_Remain_In_The_Configured_Queue()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithMaxPendingMessagesPerClient(2));
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Receive<MqttPublishPacket>(client);
        for (var i = 0; i < 2; i++)
        {
            await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        }

        await client.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(client);
        Assert.AreEqual(2L, (await server.GetSessionsAsync()).Single().PendingApplicationMessagesCount);
    }

    [TestMethod]
    public async Task Mqtt_311_Is_Not_Quota_Limited()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver", version: MqttProtocolVersion.V311);
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Receive<MqttPublishPacket>(client);
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task Suppressed_Publish_Does_Not_Consume_Quota()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer();
        var suppress = true;
        server.InterceptingOutboundPacketAsync += args =>
        {
            if (args.Packet is MqttPublishPacket && suppress)
            {
                suppress = false;
                args.ProcessPacket = false;
            }

            return Task.CompletedTask;
        };
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.AtLeastOnce);
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    public async Task Error_PubComp_Releases_Quota()
    {
        using var environment = new TestEnvironment();
        await environment.StartServer();
        var client = await Connect(environment, "receiver");
        await Subscribe(client, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        var first = await Receive<MqttPublishPacket>(client);
        await client.SendAsync(new MqttPubRecPacket { PacketIdentifier = first.PacketIdentifier });
        await Receive<MqttPubRelPacket>(client);
        await Inject(environment, "flow", MqttQualityOfServiceLevel.ExactlyOnce);
        await client.SendAsync(new MqttPubCompPacket { PacketIdentifier = first.PacketIdentifier, ReasonCode = MqttPubCompReasonCode.PacketIdentifierNotFound });
        await Receive<MqttPublishPacket>(client);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Failed_Send_Closes_Connection_Instead_Of_Stalling_Quota(bool cancelSend)
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer();
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientDisconnectedAsync += _ => { disconnected.TrySetResult(); return Task.CompletedTask; };
        server.InterceptingOutboundPacketAsync += args =>
        {
            if (args.Packet is MqttPublishPacket publish)
            {
                if (cancelSend)
                {
                    // Unexpected send cancellation must also end the connection.
                    throw new OperationCanceledException();
                }

                // Force an encoding failure after the quota slot has been consumed.
                publish.Topic = new string('a', 65536);
            }

            return Task.CompletedTask;
        };
        await Connect(environment, "receiver");
        var session = (await server.GetSessionsAsync()).Single();
        var delivery = session.DeliverApplicationMessageAsync(
            new MqttApplicationMessageBuilder().WithTopic("flow").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
        if (cancelSend)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await delivery);
        }
        else
        {
            await Assert.ThrowsAsync<MqttProtocolViolationException>(async () => await delivery);
        }
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsEmpty(await server.GetClientsAsync());
    }

    static async Task<ILowLevelMqttClient> Connect(TestEnvironment environment, string id, ushort maximum = 1,
        bool clean = true, MqttProtocolVersion version = MqttProtocolVersion.V500)
    {
        var client = environment.CreateLowLevelClient();
        var builder = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", environment.ServerPort)
            .WithClientId(id).WithProtocolVersion(version).WithCleanSession(clean);
        if (version == MqttProtocolVersion.V500)
        {
            builder.WithSessionExpiryInterval(60).WithReceiveMaximum(maximum);
        }

        var options = builder.Build();
        await client.ConnectAsync(options);
        await client.SendAsync(MqttConnectPacketFactory.Create(options));
        await Receive<MqttConnAckPacket>(client);
        return client;
    }

    static async Task<T> Receive<T>(ILowLevelMqttClient client) where T : MqttPacket
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var packet = await client.ReceiveAsync(timeout.Token);
        Assert.IsInstanceOfType<T>(packet);
        return (T)packet;
    }

    static async Task AssertPublishRemainsBlocked(TestEnvironment environment, ILowLevelMqttClient client)
    {
        await client.SendAsync(MqttPingReqPacket.Instance);
        await Receive<MqttPingRespPacket>(client);
        Assert.AreEqual(1L, (await environment.Server.GetSessionsAsync()).Single().PendingApplicationMessagesCount);
    }

    static async Task Subscribe(ILowLevelMqttClient client, string topic, MqttQualityOfServiceLevel qos)
    {
        await client.SendAsync(new MqttSubscribePacket
        {
            PacketIdentifier = 1,
            TopicFilters = [new MqttTopicFilter { Topic = topic, QualityOfServiceLevel = qos }]
        });
        await Receive<MqttSubAckPacket>(client);
    }

    static Task Inject(TestEnvironment environment, string topic, MqttQualityOfServiceLevel qos, bool retain = false)
    {
        return environment.Server.InjectApplicationMessage(new InjectedMqttApplicationMessage(
            new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload("test").WithQualityOfServiceLevel(qos).WithRetainFlag(retain).Build()));
    }
}
