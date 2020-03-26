﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public class ManagedMqttClient_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Drop_New_Messages_On_Full_Queue()
        {
            var factory = new MqttFactory();
            var managedClient = factory.CreateManagedMqttClient();
            try
            {
                var clientOptions = new ManagedMqttClientOptionsBuilder()
                    .WithMaxPendingMessages(5)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage);

                clientOptions.WithClientOptions(o => o.WithTcpServer("localhost"));

                await managedClient.StartAsync(clientOptions.Build());

                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "1" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "2" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "3" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "4" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "5" });

                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "6" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "7" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "8" });

                Assert.AreEqual(5, managedClient.PendingApplicationMessagesCount);
            }
            finally
            {
                await managedClient.StopAsync();
            }
        }

        [TestMethod]
        public async Task ManagedClients_Will_Message_Send()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                var factory = new MqttFactory();

                await testEnvironment.StartServerAsync();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort)
                    .WithWillMessage(willMessage);
                var dyingClient = testEnvironment.CreateClient();
                var dyingManagedClient = new ManagedMqttClient(dyingClient, testEnvironment.ClientLogger.CreateChildLogger());
                await dyingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions)
                    .Build());

                var recievingClient = await testEnvironment.ConnectClientAsync();
                await recievingClient.SubscribeAsync("My/last/will");

                recievingClient.UseApplicationMessageReceivedHandler(context => Interlocked.Increment(ref receivedMessagesCount));

                dyingManagedClient.Dispose();

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Start_Stop()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var factory = new MqttFactory();

                var server = await testEnvironment.StartServerAsync();

                var managedClient = new ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetLogger().CreateChildLogger());
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort);

                var connected = GetConnectedTask(managedClient);

                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions)
                    .Build());

                await connected;

                await managedClient.StopAsync();

                await Task.Delay(500);

                Assert.AreEqual(0, (await server.GetClientStatusAsync()).Count);
            }
        }

        [TestMethod]
        public async Task Storage_Queue_Drains()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;
                testEnvironment.IgnoreServerLogErrors = true;

                var factory = new MqttFactory();

                var server = await testEnvironment.StartServerAsync();

                var managedClient = new ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetLogger().CreateChildLogger());
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort);
                var storage = new ManagedMqttClientTestStorage();

                var connected = GetConnectedTask(managedClient);

                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions)
                    .WithStorage(storage)
                    .WithAutoReconnectDelay(System.TimeSpan.FromSeconds(5))
                    .Build());

                await connected;

                await testEnvironment.Server.StopAsync();

                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "1" });

                //Message should have been added to the storage queue in PublishAsync,
                //and we are awaiting PublishAsync, so the message should already be
                //in storage at this point (i.e. no waiting).
                Assert.AreEqual(1, storage.GetMessageCount());

                connected = GetConnectedTask(managedClient);

                await testEnvironment.Server.StartAsync(new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(testEnvironment.ServerPort).Build());

                await connected;

                //Wait 500ms here so the client has time to publish the queued message
                await Task.Delay(500);

                Assert.AreEqual(0, storage.GetMessageCount());

                await managedClient.StopAsync();
            }
        }

        [TestMethod]
        public async Task Subscriptions_And_Unsubscriptions_Are_Made_And_Reestablished_At_Reconnect()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var unmanagedClient = testEnvironment.CreateClient();
                var managedClient = await CreateManagedClientAsync(testEnvironment, unmanagedClient);

                var received = SetupReceivingOfMessages(managedClient, 2);

                // Perform some opposing subscriptions and unsubscriptions to verify
                // that these conflicting subscriptions are handled correctly
                await managedClient.SubscribeAsync("keptSubscribed");
                await managedClient.SubscribeAsync("subscribedThenUnsubscribed");

                await managedClient.UnsubscribeAsync("subscribedThenUnsubscribed");
                await managedClient.UnsubscribeAsync("unsubscribedThenSubscribed");

                await managedClient.SubscribeAsync("unsubscribedThenSubscribed");

                //wait a bit for the subscriptions to become established before the messages are published
                await Task.Delay(500);

                var sendingClient = await testEnvironment.ConnectClientAsync();

                async Task PublishMessages()
                {
                    await sendingClient.PublishAsync("keptSubscribed", new byte[] { 1 });
                    await sendingClient.PublishAsync("subscribedThenUnsubscribed", new byte[] { 1 });
                    await sendingClient.PublishAsync("unsubscribedThenSubscribed", new byte[] { 1 });
                }

                await PublishMessages();

                async Task AssertMessagesReceived()
                {
                    var messages = await received;
                    Assert.AreEqual("keptSubscribed", messages[0].Topic);
                    Assert.AreEqual("unsubscribedThenSubscribed", messages[1].Topic);
                }

                await AssertMessagesReceived();

                var connected = GetConnectedTask(managedClient);

                await unmanagedClient.DisconnectAsync();

                // the managed client has to reconnect by itself
                await connected;

                // wait a bit so that the managed client can reestablish the subscriptions
                await Task.Delay(500);

                received = SetupReceivingOfMessages(managedClient, 2);

                await PublishMessages();

                // and then the same subscriptions need to exist again
                await AssertMessagesReceived();
            }
        }

        // This case also serves as a regression test for the previous behavior which re-published
        // each and every existing subscriptions with every new subscription that was made
        // (causing performance problems and having the visible symptom of retained messages being received again)
        [TestMethod]
        public async Task Subscriptions_Subscribe_Only_New_Subscriptions()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var managedClient = await CreateManagedClientAsync(testEnvironment);

                var sendingClient = await testEnvironment.ConnectClientAsync();

                await managedClient.SubscribeAsync("topic");

                //wait a bit for the subscription to become established
                await Task.Delay(500);

                await sendingClient.PublishAsync(new MqttApplicationMessage { Topic = "topic", Payload = new byte[] { 1 }, Retain = true });

                var messages = await SetupReceivingOfMessages(managedClient, 1);

                Assert.AreEqual(1, messages.Count);
                Assert.AreEqual("topic", messages.Single().Topic);

                await managedClient.SubscribeAsync("anotherTopic");

                await Task.Delay(500);

                // The subscription of the other topic must not trigger a re-subscription of the existing topic
                // (and thus renewed receiving of the retained message)
                Assert.AreEqual(1, messages.Count);
            }
        }

        // This case also serves as a regression test for the previous behavior
        // that subscriptions were only published at the ConnectionCheckInterval
        [TestMethod]
        public async Task Subscriptions_Are_Published_Immediately()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                // Use a long connection check interval to verify that the subscriptions
                // do not depend on the connection check interval anymore
                var connectionCheckInterval = TimeSpan.FromSeconds(10);
                var managedClient = await CreateManagedClientAsync(testEnvironment, null, connectionCheckInterval);
                var sendingClient = await testEnvironment.ConnectClientAsync();
                await sendingClient.PublishAsync(new MqttApplicationMessage { Topic = "topic", Payload = new byte[] { 1 }, Retain = true });

                await managedClient.SubscribeAsync("topic");

                var subscribeTime = DateTime.UtcNow;

                var messages = await SetupReceivingOfMessages(managedClient, 1);

                var elapsed = DateTime.UtcNow - subscribeTime;
                Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Subscriptions must be activated immediately, this one took {elapsed}");
                Assert.AreEqual(messages.Single().Topic, "topic");
            }
        }

        [TestMethod]
        public async Task Subscriptions_Are_Cleared_At_Logout()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var managedClient = await CreateManagedClientAsync(testEnvironment);

                var sendingClient = await testEnvironment.ConnectClientAsync();
                await sendingClient.PublishAsync(new MqttApplicationMessage
                { Topic = "topic", Payload = new byte[] { 1 }, Retain = true });

                // Wait a bit for the retained message to be available
                await Task.Delay(500);

                await managedClient.SubscribeAsync("topic");

                await SetupReceivingOfMessages(managedClient, 1);

                await managedClient.StopAsync();

                var clientOptions = new MqttClientOptionsBuilder()
                  .WithTcpServer("localhost", testEnvironment.ServerPort);
                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                  .WithClientOptions(clientOptions)
                  .WithAutoReconnectDelay(TimeSpan.FromSeconds(1))
                  .Build());

                var messages = new List<MqttApplicationMessage>();
                managedClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(r =>
                {
                    messages.Add(r.ApplicationMessage);
                });

                await Task.Delay(500);

                // After reconnect and then some delay, the retained message must not be received,
                // showing that the subscriptions were cleared
                Assert.AreEqual(0, messages.Count);
            }
        }

        private async Task<ManagedMqttClient> CreateManagedClientAsync(
          TestEnvironment testEnvironment,
          IMqttClient underlyingClient = null,
          TimeSpan? connectionCheckInterval = null)
        {
            await testEnvironment.StartServerAsync();

            var clientOptions = new MqttClientOptionsBuilder()
              .WithTcpServer("localhost", testEnvironment.ServerPort);

            var managedOptions = new ManagedMqttClientOptionsBuilder()
              .WithClientOptions(clientOptions)
              .Build();

            // Use a short connection check interval so that subscription operations are performed quickly
            // in order to verify against a previous implementation that performed subscriptions only
            // at connection check intervals
            managedOptions.ConnectionCheckInterval = connectionCheckInterval ?? TimeSpan.FromSeconds(0.1);

            var managedClient =
              new ManagedMqttClient(underlyingClient ?? testEnvironment.CreateClient(), new MqttNetLogger().CreateChildLogger());

            var connected = GetConnectedTask(managedClient);

            await managedClient.StartAsync(managedOptions);

            await connected;

            return managedClient;
        }

        /// <summary>
        /// Returns a task that will finish when the <paramref name="managedClient"/> has connected
        /// </summary>
        private Task GetConnectedTask(ManagedMqttClient managedClient)
        {
            TaskCompletionSource<bool> connected = new TaskCompletionSource<bool>();
            managedClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(e =>
            {
                managedClient.ConnectedHandler = null;
                connected.SetResult(true);
            });
            return connected.Task;
        }

        /// <summary>
        /// Returns a task that will return the messages received on <paramref name="managedClient"/>
        /// when <paramref name="expectedNumberOfMessages"/> have been received
        /// </summary>
        private Task<List<MqttApplicationMessage>> SetupReceivingOfMessages(ManagedMqttClient managedClient, int expectedNumberOfMessages)
        {
            var receivedMessages = new List<MqttApplicationMessage>();
            var allReceived = new TaskCompletionSource<List<MqttApplicationMessage>>();
            managedClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(r =>
            {
                receivedMessages.Add(r.ApplicationMessage);
                if (receivedMessages.Count == expectedNumberOfMessages)
                {
                    allReceived.SetResult(receivedMessages);
                }
            });
            return allReceived.Task;
        }
    }

    public class ManagedMqttClientTestStorage : IManagedMqttClientStorage
    {
        private IList<ManagedMqttApplicationMessage> _messages = null;

        public Task<IList<ManagedMqttApplicationMessage>> LoadQueuedMessagesAsync()
        {
            if (_messages == null)
            {
                _messages = new List<ManagedMqttApplicationMessage>();
            }
            return Task.FromResult(_messages);
        }

        public Task SaveQueuedMessagesAsync(IList<ManagedMqttApplicationMessage> messages)
        {
            _messages = messages;
            return Task.FromResult(0);
        }

        public int GetMessageCount()
        {
            return _messages.Count;
        }
    }
}
