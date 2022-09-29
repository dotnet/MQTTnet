// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Implementations;

namespace MQTTnet.Tests.ManagedMqttClient
{
    [TestClass]
    public sealed class ManagedMqttClient_Event_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Connecting_Should_Fire_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedClient();

                var eventFired = false;
                managedClient.ConnectedAsync += e =>
                {
                    eventFired = true;
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.StartAsync(testEnvironment.CreateDefaultManagedMqttClientOptions());

                await LongTestDelay();

                Assert.IsTrue(eventFired);
            }
        }

        [TestMethod]
        public async Task Connecting_To_Invalid_Server_Should_Fire_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var managedClient = testEnvironment.CreateManagedMqttClient();

                ConnectingFailedEventArgs connectingFailedEventArgs = null;
                managedClient.ConnectingFailedAsync += args =>
                {
                    connectingFailedEventArgs = args;
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.StartAsync(
                    new ManagedMqttClientOptions
                    {
                        ClientOptions = testEnvironment.Factory.CreateClientOptionsBuilder().WithTimeout(TimeSpan.FromSeconds(2)).WithTcpServer("wrong_server", 1234).Build()
                    });

                SpinWait.SpinUntil(() => connectingFailedEventArgs != null, 10000);

                // The wrong server must be reported in general.
                Assert.IsNotNull(connectingFailedEventArgs);
                Assert.IsNotNull(connectingFailedEventArgs.Exception);
                Assert.IsNull(connectingFailedEventArgs.ConnectResult);
            }
        }

        [TestMethod]
        public async Task Disconnect_Should_Fire_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedClient();

                var eventFired = false;
                managedClient.DisconnectedAsync += e =>
                {
                    eventFired = true;
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.StartAsync(testEnvironment.CreateDefaultManagedMqttClientOptions());

                await LongTestDelay();

                await server.StopAsync();

                await LongTestDelay();

                Assert.IsTrue(eventFired);
            }
        }

        [TestMethod]
        public async Task Enqueueing_Should_Fire_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedClient();

                var eventFired = false;
                managedClient.ApplicationMessageEnqueueingAsync += e =>
                {
                    eventFired = true;
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.EnqueueAsync("A");

                Assert.IsTrue(eventFired);
            }
        }

        [TestMethod]
        public async Task Subscription_Should_Fire_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedClient();

                await managedClient.SubscribeAsync("A");

                var eventFired = false;
                managedClient.SubscribeProcessedAsync += e =>
                {
                    eventFired = e.Options.TopicFilters[0].Topic == "A";
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.StartAsync(testEnvironment.CreateDefaultManagedMqttClientOptions());

                await LongTestDelay();

                Assert.IsTrue(eventFired);
            }
        }

        [TestMethod]
        public async Task Unsubscription_Should_Fire_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedClient();

                await managedClient.SubscribeAsync("A");

                var eventFired = false;
                managedClient.UnsubscribeProcessedAsync += e =>
                {
                    eventFired = e.Options.TopicFilters[0] == "A";
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.StartAsync(testEnvironment.CreateDefaultManagedMqttClientOptions());

                await LongTestDelay();

                await managedClient.UnsubscribeAsync("A");

                await LongTestDelay();

                Assert.IsTrue(eventFired);
            }
        }
    }
}