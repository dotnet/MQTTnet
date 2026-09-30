// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Server;
using MQTTnet.Server.Exceptions;

namespace MQTTnet.Tests.Internal;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttPacketBusItem_Tests
{
    [TestMethod]
    public void Fail_Without_Waiter_Does_Not_Raise_Unobserved_Exception()
    {
        var exception = new MqttPendingMessagesOverflowException("test", MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage);
        var unobservedExceptions = 0;

        void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.Flatten().InnerExceptions.Contains(exception))
            {
                Interlocked.Increment(ref unobservedExceptions);
                args.SetObserved();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        try
        {
            var taskReference = CreateFailedPacketBusItem(exception);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(taskReference.IsAlive, "The faulted task must be collected to exercise unobserved exception reporting.");
            Assert.AreEqual(0, unobservedExceptions);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Wait_Failed_Packet_Bus_Item_Preserves_Exception(bool waitBeforeFailure)
    {
        var item = new MqttPacketBusItem(new MqttPublishPacket());
        var exception = new MqttPendingMessagesOverflowException("test", MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage);
        var task = waitBeforeFailure ? item.WaitAsync() : null;

        item.Fail(exception);

        task ??= item.WaitAsync();
        var thrownException = await Assert.ThrowsExactlyAsync<MqttPendingMessagesOverflowException>(async () => await task);

        Assert.AreSame(exception, thrownException);
        Assert.IsTrue(task.IsFaulted);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference CreateFailedPacketBusItem(Exception exception)
    {
        // Keep strong references out of the collecting method, including in debug builds.
        var item = new MqttPacketBusItem(new MqttPublishPacket());
        item.Fail(exception);
        return new WeakReference(item.WaitAsync());
    }
}