// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public class RoundtripTime_Tests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Round_Trip_Time()
    {
        using var testEnvironment = new TestEnvironment(TestContext);
        await testEnvironment.StartServer();

        var receiverClient = await testEnvironment.ConnectClient();
        var senderClient = await testEnvironment.ConnectClient();

        TaskCompletionSource<string> response = null;

        receiverClient.ApplicationMessageReceivedAsync += e =>
        {
            response?.TrySetResult(e.ApplicationMessage.ConvertPayloadToString());
            return CompletedTask.Instance;
        };

        await receiverClient.SubscribeAsync("#");

        var times = new List<TimeSpan>();
        var stopwatch = Stopwatch.StartNew();

        await Task.Delay(1000);

        for (var i = 0; i < 100; i++)
        {
            response = new TaskCompletionSource<string>();
            await senderClient.PublishStringAsync("test", DateTime.UtcNow.Ticks.ToString());
            if (!response.Task.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException();
            }

            stopwatch.Stop();
            times.Add(stopwatch.Elapsed);
            stopwatch.Restart();
        }
    }
}