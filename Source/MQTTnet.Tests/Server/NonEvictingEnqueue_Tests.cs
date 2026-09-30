// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Exceptions;
using MQTTnet.Server.Internal;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class NonEvictingEnqueue_Tests
{
    static readonly string[] RecoveryTopics = ["old", "new"];
    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtMostOnce, 1)]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce, 1)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce, 1)]
    [DataRow(MqttQualityOfServiceLevel.AtMostOnce, 2)]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce, 2)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce, 2)]
    public async Task Full_Queue_Rejection_Preserves_Contents_Identifiers_And_Tracking(MqttQualityOfServiceLevel qos, int capacity)
    {
        using var context = new Context(capacity);
        var overwritten = 0;
        context.Events.QueuedApplicationMessageOverwrittenEvent.AddHandler(_ => Interlocked.Increment(ref overwritten));
        for (var i = 0; i < capacity; i++)
        {
            Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("old-" + i, qos), out var accepted, false));
            Assert.AreEqual(qos == MqttQualityOfServiceLevel.AtMostOnce ? 0 : i + 1, (int)accepted.PacketIdentifier);
        }

        for (var i = 0; i < 32; i++)
        {
            Assert.IsFalse(context.Status.TryEnqueueApplicationMessage(Message("retry", qos), out var rejected, false));
            Assert.IsNull(rejected);
        }

        Assert.AreEqual(capacity, (int)context.Session.PendingDataPacketsCount);
        Assert.AreEqual(0, overwritten);
        if (qos == MqttQualityOfServiceLevel.AtMostOnce) Assert.IsNull(context.Session.PeekAcknowledgePublishPacket(0));
        Assert.IsNull(context.Session.PeekAcknowledgePublishPacket((ushort)(capacity + 1)));
        for (var i = 0; i < capacity; i++)
        {
            var item = await context.Take();
            var packet = (MqttPublishPacket)item.Packet;
            Assert.AreEqual("old-" + i, packet.Topic);
            if (qos != MqttQualityOfServiceLevel.AtMostOnce)
            {
                Assert.AreSame(packet, context.Session.AcknowledgePublishPacket(packet.PacketIdentifier));
                Assert.IsNull(context.Session.AcknowledgePublishPacket(packet.PacketIdentifier));
            }
        }

        Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("retry", MqttQualityOfServiceLevel.AtLeastOnce), out var retry, false));
        Assert.AreEqual(qos == MqttQualityOfServiceLevel.AtMostOnce ? 1 : capacity + 1, (int)retry.PacketIdentifier);
        var retriedPacket = (MqttPublishPacket)(await context.Take()).Packet;
        Assert.AreSame(retriedPacket, context.Session.PeekAcknowledgePublishPacket(retry.PacketIdentifier));
    }

    [TestMethod]
    public void Rejection_Does_Not_Create_Unobserved_Overflow_Failures()
    {
        using var context = new Context(1);
        var failures = 0;
        void OnUnobserved(object sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.InnerExceptions.OfType<MqttPendingMessagesOverflowException>().Any(e => e.SessionId == context.Session.Id))
            {
                Interlocked.Increment(ref failures);
                args.SetObserved();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("old"), out _, false));
            for (var i = 0; i < 128; i++)
            {
                Assert.IsFalse(context.Status.TryEnqueueApplicationMessage(Message("rejected"), out _, false));
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.AreEqual(0, Volatile.Read(ref failures));
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Concurrent_Producers_Cannot_Both_Claim_One_Slot(bool includeLegacyProducer)
    {
        using var context = new Context(1, MqttPendingMessagesOverflowStrategy.DropNewMessage);
        for (var round = 0; round < 64; round++)
        {
            using var barrier = new Barrier(3);
            var accepted = 0;
            var tasks = Enumerable.Range(0, 2).Select(index => Task.Run(() =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                bool success;
                if (includeLegacyProducer && index == 0)
                {
                    var item = new MqttPacketBusItem(new MqttPublishPacket { Topic = "legacy" });
                    success = context.Session.EnqueueDataPacket(item) == EnqueueDataPacketResult.Enqueued;
                    if (!success)
                    {
                        _ = item.WaitAsync().Exception; // Expected legacy DropNew failure.
                    }
                }
                else
                {
                    success = context.Status.TryEnqueueApplicationMessage(Message("bounded"), out _, false);
                }

                if (success) Interlocked.Increment(ref accepted);
            })).ToArray();
            Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(1, accepted);
            Assert.AreEqual(1, (int)context.Session.PendingDataPacketsCount);
            await context.Take();
        }
    }

    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Admission_And_Recovery_Do_Not_Lose_A_Tracked_Message(MqttQualityOfServiceLevel qos)
    {
        using var context = new Context(2);
        for (var round = 0; round < 64; round++)
        {
            Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("old", qos), out _, false));
            using var barrier = new Barrier(3);
            var recovering = Task.Run(() => { barrier.SignalAndWait(TimeSpan.FromSeconds(5)); context.Session.Recover(); });
            var admitting = Task.Run(() => { barrier.SignalAndWait(TimeSpan.FromSeconds(5)); return context.Status.TryEnqueueApplicationMessage(Message("new", qos), out _, false); });
            barrier.SignalAndWait(TimeSpan.FromSeconds(5));
            await Task.WhenAll(recovering, admitting).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(admitting.Result);
            Assert.AreEqual(2, (int)context.Session.PendingDataPacketsCount);
            var topics = new List<string>();
            for (var i = 0; i < 2; i++)
            {
                var packet = (MqttPublishPacket)(await context.Take()).Packet;
                topics.Add(packet.Topic);
                Assert.AreSame(packet, context.Session.AcknowledgePublishPacket(packet.PacketIdentifier));
            }

            CollectionAssert.AreEquivalent(RecoveryTopics, topics);
        }
    }

    [TestMethod]
    public async Task Admission_And_Disposal_Have_No_Partial_Admission()
    {
        for (var round = 0; round < 32; round++)
        {
            using var context = new Context(1);
            using var barrier = new Barrier(3);
            var disposing = Task.Run(() => { barrier.SignalAndWait(TimeSpan.FromSeconds(5)); context.Session.Dispose(); });
            var admitting = Task.Run(() =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                try { return context.Status.TryEnqueueApplicationMessage(Message("one", MqttQualityOfServiceLevel.AtLeastOnce), out _, false); }
                catch (ObjectDisposedException) { return false; }
            });
            barrier.SignalAndWait(TimeSpan.FromSeconds(5));
            await Task.WhenAll(disposing, admitting).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(admitting.Result ? 1 : 0, (int)context.Session.PendingDataPacketsCount);
            Assert.AreEqual(admitting.Result, context.Session.PeekAcknowledgePublishPacket(1) != null);
            Assert.ThrowsExactly<ObjectDisposedException>(() => context.Status.TryEnqueueApplicationMessage(Message("late"), out _, false));
            Assert.ThrowsExactly<ObjectDisposedException>(() => context.Session.Recover());
        }
    }

    [TestMethod]
    public async Task Control_And_Health_Packets_Bypass_Full_Data_Queue()
    {
        using var context = new Context(1);
        Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("data"), out _, false));
        context.Session.EnqueueHealthPacket(new MqttPacketBusItem(MqttPingRespPacket.Instance));
        context.Session.EnqueueControlPacket(new MqttPacketBusItem(new MqttPubRelPacket { PacketIdentifier = 42 }));
        Assert.IsFalse(context.Status.TryEnqueueApplicationMessage(Message("blocked"), out _, false));
        var packets = new List<MqttPacket>();
        for (var i = 0; i < 3; i++) packets.Add((await context.Take()).Packet);
        Assert.AreEqual(1, packets.OfType<MqttPublishPacket>().Count());
        Assert.AreEqual(1, packets.OfType<MqttPingRespPacket>().Count());
        Assert.AreEqual(1, packets.OfType<MqttPubRelPacket>().Count());
    }

    [TestMethod]
    public async Task Legacy_Eviction_Notification_Runs_Outside_Admission_Gate()
    {
        using var context = new Context(1);
        var old = new MqttPacketBusItem(new MqttPublishPacket { Topic = "old" });
        context.Session.EnqueueDataPacket(old);
        var callbackProgress = false;
        context.Events.QueuedApplicationMessageOverwrittenEvent.AddHandler(args =>
        {
            var attempt = Task.Run(() => context.Status.TryEnqueueApplicationMessage(Message("probe"), out _, false));
            callbackProgress = attempt.Wait(TimeSpan.FromSeconds(2)) && !attempt.Result;
        });
        Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("legacy-new"), out _));
        Assert.IsInstanceOfType<MqttPendingMessagesOverflowException>(old.WaitAsync().Exception?.InnerException);
        Assert.IsTrue(callbackProgress);
        Assert.AreEqual("legacy-new", ((MqttPublishPacket)(await context.Take()).Packet).Topic);
    }

    [TestMethod]
    public void Recovery_Overwrite_Notifications_Run_Outside_Admission_Gate()
    {
        using var context = new Context(3);
        for (var i = 0; i < 3; i++)
        {
            Assert.IsTrue(context.Status.TryEnqueueApplicationMessage(Message("recover-" + i, MqttQualityOfServiceLevel.AtLeastOnce), out _, false));
        }

        context.Options.MaxPendingMessagesPerClient = 1;
        var calls = 0;
        var progressed = 0;
        context.Events.QueuedApplicationMessageOverwrittenEvent.AddHandler(args =>
        {
            Interlocked.Increment(ref calls);
            var attempt = Task.Run(() => context.Status.TryEnqueueApplicationMessage(Message("probe"), out _, false));
            if (attempt.Wait(TimeSpan.FromSeconds(2)) && !attempt.Result) Interlocked.Increment(ref progressed);
        });
        void ObserveExpectedLegacyOverflow(object sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.InnerExceptions.OfType<MqttPendingMessagesOverflowException>().Any(e => e.SessionId == context.Session.Id)) args.SetObserved();
        }

        TaskScheduler.UnobservedTaskException += ObserveExpectedLegacyOverflow;
        try
        {
            context.Session.Recover();
            Assert.AreEqual(2, calls);
            Assert.AreEqual(2, progressed);
            Assert.AreEqual(1, (int)context.Session.PendingDataPacketsCount);
            // Recover deliberately applies the legacy overflow policy; observe only these expected failures.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= ObserveExpectedLegacyOverflow;
        }
    }

    [TestMethod]
    public void Null_Message_Is_Rejected_Before_Mutation()
    {
        using var context = new Context(1);
        Assert.ThrowsExactly<ArgumentNullException>(() => context.Status.TryEnqueueApplicationMessage(null, out _, false));
        Assert.AreEqual(0, (int)context.Session.PendingDataPacketsCount);
    }

    static MqttApplicationMessage Message(string topic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce) =>
        new MqttApplicationMessageBuilder().WithTopic(topic).WithQualityOfServiceLevel(qos).Build();

    sealed class Context : IDisposable
    {
        readonly MqttRetainedMessagesManager _retained;
        readonly MqttClientSessionsManager _sessions;
        public MqttServerEventContainer Events { get; } = new();
        public MqttSession Session { get; }
        public MqttSessionStatus Status { get; }
        public MqttServerOptions Options { get; }
        public Context(int capacity, MqttPendingMessagesOverflowStrategy strategy = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
        {
            var options = new MqttServerOptionsBuilder().WithMaxPendingMessagesPerClient(capacity).WithPendingMessagesOverflowStrategy(strategy).Build();
            Options = options;
            var logger = new MqttNetNullLogger();
            _retained = new MqttRetainedMessagesManager(Events, logger);
            _sessions = new MqttClientSessionsManager(options, _retained, Events, logger);
            Session = new MqttSession(new MqttConnectPacket { ClientId = "admission-" + Guid.NewGuid() }, new Hashtable(), options, Events, _retained, _sessions);
            Status = new MqttSessionStatus(Session);
        }
        public Task<MqttPacketBusItem> Take() => Session.DequeuePacketAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        public void Dispose() { Session.Dispose(); _sessions.Dispose(); _retained.Dispose(); }
    }
}
