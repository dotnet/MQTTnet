// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Internal;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttPacketBusItem_Tests
{
    [TestMethod]
    public void Failure_Without_Waiter_Does_Not_Raise_Unobserved_Exception()
    {
        var packetFailure = new InvalidOperationException("Packet failure without a waiter");
        var controlFailure = new InvalidOperationException("Unobserved-task positive control");
        var packetEvents = 0;
        var controlEvents = 0;

        void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.InnerExceptions.Contains(packetFailure))
            {
                Interlocked.Increment(ref packetEvents);
                args.SetObserved();
            }

            if (args.Exception.InnerExceptions.Contains(controlFailure))
            {
                Interlocked.Increment(ref controlEvents);
                args.SetObserved();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedException;
        try
        {
            var packetTask = CreateUnwaitedFailure(packetFailure);
            var controlTask = CreateUnobservedControl(controlFailure);
            for (var attempt = 0; attempt < 10; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (!packetTask.IsAlive && !controlTask.IsAlive)
                {
                    break;
                }
            }

            Assert.IsFalse(packetTask.IsAlive, "Packet completion task was not collected.");
            Assert.IsFalse(controlTask.IsAlive, "Positive-control task was not collected.");
            Assert.AreEqual(1, Volatile.Read(ref controlEvents), "Unobserved-task detection must be active.");
            Assert.AreEqual(0, Volatile.Read(ref packetEvents));
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedException;
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Failure_Propagates_To_Existing_And_Late_Waiters(bool waitBeforeFailure)
    {
        var item = new MqttPacketBusItem(new MqttPublishPacket());
        var failure = new InvalidOperationException("Original send failure");
        var waiter = waitBeforeFailure ? item.WaitAsync() : null;
        item.Fail(failure);
        item.Fail(new InvalidOperationException("A later failure must not replace the original"));
        item.Cancel();
        waiter ??= item.WaitAsync();

        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => waiter);
        Assert.AreSame(failure, thrown);
        Assert.IsTrue(waiter.IsFaulted);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Failure_Does_Not_Change_An_Already_Completed_Item(bool canceled)
    {
        var packet = new MqttPublishPacket();
        var item = new MqttPacketBusItem(packet);
        if (canceled)
        {
            item.Cancel();
        }
        else
        {
            item.Complete();
        }

        item.Fail(new InvalidOperationException("Failure after completion"));
        if (canceled)
        {
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => item.WaitAsync());
        }
        else
        {
            Assert.AreSame(packet, await item.WaitAsync());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference CreateUnwaitedFailure(Exception failure)
    {
        var item = new MqttPacketBusItem(new MqttPublishPacket());
        item.Fail(failure);
        return new WeakReference(item.WaitAsync());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference CreateUnobservedControl(Exception failure)
    {
        return new WeakReference(Task.FromException(failure));
    }

    [TestMethod]
    public void Fire_Completed_Event()
    {
        var eventFired = false;

        var item = new MqttPacketBusItem(new MqttPublishPacket());
        item.Completed += (_, _) =>
        {
            eventFired = true;
        };

        item.Complete();

        Assert.IsTrue(eventFired);
    }

    [TestMethod]
    public Task Wait_Packet_Bus_Item_After_Already_Canceled()
    {
        return Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            var item = new MqttPacketBusItem(new MqttPublishPacket());

            // Finish the item before the actual
            item.Cancel();

            await item.WaitAsync();
        });
    }
}
