// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;

namespace MQTTnet.Tests.Internal;

// ReSharper disable InconsistentNaming
[TestClass]
public class AsyncQueue_Tests
{
    [TestMethod]
    public async Task Preserve_Order()
    {
        var queue = new AsyncQueue<string>();
        queue.Enqueue("1");
        queue.Enqueue("2");
        queue.Enqueue("3");

        Assert.AreEqual("1", (await queue.TryDequeueAsync(CancellationToken.None)).Item);
        Assert.AreEqual("2", (await queue.TryDequeueAsync(CancellationToken.None)).Item);
        Assert.AreEqual("3", (await queue.TryDequeueAsync(CancellationToken.None)).Item);
    }

    [TestMethod]
    public void Count()
    {
        var queue = new AsyncQueue<string>();

        queue.Enqueue("1");
        Assert.AreEqual(1, queue.Count);

        queue.Enqueue("2");
        Assert.AreEqual(2, queue.Count);

        queue.Enqueue("3");
        Assert.AreEqual(3, queue.Count);
    }

    [TestMethod]
    public async Task Cancellation()
    {
        var queue = new AsyncQueue<int>();

        bool success;
        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
        {
            success = (await queue.TryDequeueAsync(cancellationTokenSource.Token)).IsSuccess;
        }

        Assert.IsFalse(success);
    }

    [TestMethod]
    public async Task Process_Async()
    {
        var queue = new AsyncQueue<int>();

        var sum = 0;
        var worker = Task.Run(async () =>
        {
            while (sum < 6)
            {
                sum += (await queue.TryDequeueAsync(CancellationToken.None)).Item;
            }
        });

        queue.Enqueue(1);
        await Task.Delay(500);

        queue.Enqueue(2);
        await Task.Delay(500);

        queue.Enqueue(3);
        await Task.Delay(500);

        Assert.AreEqual(6, sum);
        Assert.AreEqual(TaskStatus.RanToCompletion, worker.Status);
    }

    [TestMethod]
    public async Task Process_Async_With_Initial_Delay()
    {
        var queue = new AsyncQueue<int>();

        var sum = 0;
        var worker = Task.Run(async () =>
        {
            while (sum < 6)
            {
                sum += (await queue.TryDequeueAsync(CancellationToken.None)).Item;
            }
        });

        // This line is the diff to test _Process_Async_
        await Task.Delay(500);

        queue.Enqueue(1);
        await Task.Delay(500);

        queue.Enqueue(2);
        await Task.Delay(500);

        queue.Enqueue(3);
        await Task.Delay(500);

        Assert.AreEqual(6, sum);
        Assert.AreEqual(TaskStatus.RanToCompletion, worker.Status);
    }

    [TestMethod]
    public void Dequeue_Sync()
    {
        var queue = new AsyncQueue<string>();
        queue.Enqueue("1");
        queue.Enqueue("2");
        queue.Enqueue("3");

        Assert.AreEqual("1", queue.TryDequeue().Item);
        Assert.AreEqual("2", queue.TryDequeue().Item);
        Assert.AreEqual("3", queue.TryDequeue().Item);
    }

    [TestMethod]
    public void Clear()
    {
        var queue = new AsyncQueue<string>();
        queue.Enqueue("1");
        queue.Enqueue("2");
        queue.Enqueue("3");

        queue.Clear();
        Assert.AreEqual(0, queue.Count);

        queue.Enqueue("4");

        Assert.AreEqual(1, queue.Count);
        Assert.AreEqual("4", queue.TryDequeue().Item);
    }
}