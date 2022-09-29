// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.Tests.ManagedMqttClient
{
    [TestClass]
    public sealed class ManagedMqttClient_Messages_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Drop_New_Messages_On_Full_Queue()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedMqttClient();

                var clientOptions = new ManagedMqttClientOptionsBuilder().WithClientOptions(o => o.WithTcpServer("localhost"))
                    .WithMaxPendingMessages(5)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage);

                await managedClient.StartAsync(clientOptions.Build());

                await managedClient.EnqueueAsync("1");
                await managedClient.EnqueueAsync("2");
                await managedClient.EnqueueAsync("3");
                await managedClient.EnqueueAsync("4");
                await managedClient.EnqueueAsync("5");
                await managedClient.EnqueueAsync("6");
                await managedClient.EnqueueAsync("7");
                await managedClient.EnqueueAsync("8");

                Assert.AreEqual(5, managedClient.EnqueuedApplicationMessagesCount);
            }
        }

        [TestMethod]
        public async Task Drop_Old_Messages_On_Full_Queue()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var managedClient = testEnvironment.CreateManagedMqttClient();

                var clientOptions = new ManagedMqttClientOptionsBuilder().WithClientOptions(o => o.WithTcpServer("localhost"))
                    .WithMaxPendingMessages(5)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage);

                await managedClient.StartAsync(clientOptions.Build());

                await managedClient.EnqueueAsync("1");
                await managedClient.EnqueueAsync("2");
                await managedClient.EnqueueAsync("3");
                await managedClient.EnqueueAsync("4");
                await managedClient.EnqueueAsync("5");
                await managedClient.EnqueueAsync("6");
                await managedClient.EnqueueAsync("7");
                await managedClient.EnqueueAsync("8");

                Assert.AreEqual(5, managedClient.EnqueuedApplicationMessagesCount);
            }
        }

        [TestMethod]
        public async Task Preserve_Order_Of_Subscriptions()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                // Prepare the retained messages.
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("A").WithPayload("P").WithRetainFlag().Build());
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("B").WithPayload("P").WithRetainFlag().Build());
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("C").WithPayload("P").WithRetainFlag().Build());
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("3").WithPayload("P").WithRetainFlag().Build());
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("2").WithPayload("P").WithRetainFlag().Build());
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("1").WithPayload("P").WithRetainFlag().Build());

                // Subscribe to the retained messages in a defined order.
                var managedClient = testEnvironment.CreateManagedMqttClient();

                var applicationMessages = new List<MqttApplicationMessage>();
                managedClient.ApplicationMessageReceivedAsync += e =>
                {
                    applicationMessages.Add(e.ApplicationMessage);
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.SubscribeAsync("B");
                await managedClient.SubscribeAsync("C");
                await managedClient.SubscribeAsync("A");
                await managedClient.SubscribeAsync("2");
                await managedClient.SubscribeAsync("1");
                await managedClient.SubscribeAsync("3");

                await managedClient.StartAsync(testEnvironment.CreateDefaultManagedMqttClientOptions()).ConfigureAwait(false);

                await LongTestDelay();

                var order = string.Join(string.Empty, applicationMessages.Select(a => a.Topic));

                Assert.AreEqual("BCA213", order);
            }
        }

        [TestMethod]
        public async Task Publish()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.TrackServerReceivedApplicationMessages = true;
                await testEnvironment.StartServer();

                var managedClient = await testEnvironment.StartManagedClient();

                await managedClient.EnqueueAsync(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("TEST").Build());

                await LongTestDelay();

                Assert.AreEqual(1, testEnvironment.ServerReceivedApplicationMessages.Count);
            }
        }
    }
}