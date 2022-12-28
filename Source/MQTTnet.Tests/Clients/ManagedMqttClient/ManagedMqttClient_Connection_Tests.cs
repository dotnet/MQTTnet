// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
namespace MQTTnet.Tests.ManagedMqttClient
========
namespace MQTTnet.Tests.Clients.ManagedMqttClient
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
{
    [TestClass]
    public sealed class ManagedMqttClient_Connection_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Receive_While_Not_Cleanly_Disconnected()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                var senderClient = await testEnvironment.ConnectClient();

                // Prepare managed client.
                var managedClient = testEnvironment.Factory.CreateManagedMqttClient();
                await managedClient.SubscribeAsync("#");
                var receivedMessages = testEnvironment.CreateApplicationMessageHandler(managedClient);
                
                var managedClientOptions = new ManagedMqttClientOptions
                {
                    ClientOptions = testEnvironment.Factory.CreateClientOptionsBuilder()
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .WithClientId(nameof(Receive_While_Not_Cleanly_Disconnected) + "_managed")
                        .WithCleanSession(false)
                        .Build()
                };

                await managedClient.StartAsync(managedClientOptions);
                await LongTestDelay();
                await LongTestDelay();
                
                // Send test data.
                await senderClient.PublishStringAsync("topic1");
                await LongTestDelay();
                await LongTestDelay();
                
                receivedMessages.AssertReceivedCountEquals(1);

                // Stop the managed client but keep session at server (not clean disconnect required).
                await managedClient.StopAsync(false);
                await LongTestDelay();
                
                // Send new messages in the meantime.
                await senderClient.PublishStringAsync("topic2", qualityOfServiceLevel: MqttQualityOfServiceLevel.ExactlyOnce);
                await LongTestDelay();
                
                // Start the managed client, it should receive the new message.
                await managedClient.StartAsync(managedClientOptions);
                await LongTestDelay();
                
                receivedMessages.AssertReceivedCountEquals(2);
                
                // Stop and start again, no new message should be received.
                for (var i = 0; i < 3; i++)
                {
                    await managedClient.StopAsync(false);
                    await LongTestDelay();
                    await managedClient.StartAsync(managedClientOptions);
                    await LongTestDelay();
                }

                receivedMessages.AssertReceivedCountEquals(2);
            }
        }

        [TestMethod]
        public async Task Connect_To_Invalid_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                await LongTestDelay();

                var managedClient = testEnvironment.Factory.CreateManagedMqttClient(testEnvironment.CreateClient());

                ConnectingFailedEventArgs connectingFailedEventArgs = null;
                managedClient.ConnectingFailedAsync += args =>
                {
                    connectingFailedEventArgs = args;
                    return CompletedTask.Instance;
                };

                await managedClient.StartAsync(
                    new ManagedMqttClientOptions
                    {
                        ClientOptions = testEnvironment.Factory.CreateClientOptionsBuilder().WithTimeout(TimeSpan.FromSeconds(2)).WithTcpServer("wrong_server", 1234).Build()
                    });

                await managedClient.EnqueueAsync("test_topic_2");

                SpinWait.SpinUntil(() => connectingFailedEventArgs != null, 10000);

                // The wrong server must be reported in general.
                Assert.IsNotNull(connectingFailedEventArgs);
                Assert.IsNotNull(connectingFailedEventArgs.Exception);
                Assert.IsNull(connectingFailedEventArgs.ConnectResult);
            }
        }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                // Prepare the receiving client.
                var receivedMessagesCount = 0;
<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
========

                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithWillTopic("My/last/will")
                    .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                    .Build();

                var dyingClient = testEnvironment.CreateClient();
                var dyingManagedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(dyingClient, testEnvironment.ClientLogger);

                await dyingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

                // Wait until the managed client is fully set up and running.
                await Task.Delay(1000);

>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
                var receivingClient = await testEnvironment.ConnectClient();
                receivingClient.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return CompletedTask.Instance;
                };
                
                await receivingClient.SubscribeAsync("My/last/will");
                
                // Prepare the managed client.
                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithWillTopic("My/last/will")
                    .Build();

                var dyingClient = testEnvironment.CreateClient();
                var dyingManagedClient = testEnvironment.CreateManagedMqttClient(dyingClient);
                await dyingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

                // Wait until the managed client is fully set up and running.
                await LongTestDelay();
                
                // Disposing the client will not sent a DISCONNECT packet so that connection is terminated
                // which will lead to the will publish.
                dyingManagedClient.Dispose();

                // Wait for arrival of the will message at the receiver.
                await LongTestDelay();

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Start_Stop()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
                var managedClient = testEnvironment.CreateManagedClient();
========
                var managedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort);
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs

                var connected = GetConnectedTask(managedClient);

                await managedClient.StartAsync(testEnvironment.CreateDefaultManagedMqttClientOptions());

                await connected;

                await managedClient.StopAsync();

                await LongTestDelay();

                Assert.AreEqual(0, (await server.GetClientsAsync()).Count);
            }
        }

        [TestMethod]
        public async Task Storage_Queue_Drains()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;
                testEnvironment.IgnoreServerLogErrors = true;

                await testEnvironment.StartServer();

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
                var managedClient = testEnvironment.CreateManagedClient();
========
                var managedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort);
                var storage = new ManagedMqttClientTestStorage();

                var connected = GetConnectedTask(managedClient);

                await managedClient.StartAsync(
                    new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithStorage(storage).WithAutoReconnectDelay(TimeSpan.FromSeconds(5)).Build());

                await connected;

                await testEnvironment.Server.StopAsync();

                await managedClient.EnqueueAsync("1");

                //Message should have been added to the storage queue in PublishAsync,
                //and we are awaiting PublishAsync, so the message should already be
                //in storage at this point (i.e. no waiting).
                Assert.AreEqual(1, storage.GetMessageCount());

                connected = GetConnectedTask(managedClient);

                await testEnvironment.Server.StartAsync();

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
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                
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
                await LongTestDelay();

                var sendingClient = await testEnvironment.ConnectClient();

                async Task PublishMessages()
                {
                    await sendingClient.PublishBinaryAsync("keptSubscribed", new byte[] { 1 });
                    await sendingClient.PublishBinaryAsync("subscribedThenUnsubscribed", new byte[] { 1 });
                    await sendingClient.PublishBinaryAsync("unsubscribedThenSubscribed", new byte[] { 1 });
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
                await LongTestDelay();

                received = SetupReceivingOfMessages(managedClient, 2);

                await PublishMessages();

                // and then the same subscriptions need to exist again
                await AssertMessagesReceived();
            }
        }

        [TestMethod]
        public async Task Subscriptions_Are_Cleared_At_Logout()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer().ConfigureAwait(false);

                var sendingClient = await testEnvironment.ConnectClient().ConfigureAwait(false);
                await sendingClient.PublishStringAsync("topic", "A", retain: true);

                // Wait a bit for the retained message to be available
                await LongTestDelay();

                await sendingClient.DisconnectAsync();

                // Prepare retained messages.
                await server.InjectApplicationMessage(testEnvironment.Factory.CreateApplicationMessageBuilder().WithTopic("topic").WithPayload("payload").WithRetainFlag().Build());
                
                // Now use the managed client and check if subscriptions get cleared properly.
                var receivedManagedMessages = new List<MqttApplicationMessage>();
                
                var managedClient = testEnvironment.Factory.CreateManagedMqttClient(testEnvironment.CreateClient());
                managedClient.ApplicationMessageReceivedAsync += e =>
                {
                    receivedManagedMessages.Add(e.ApplicationMessage);
                    return CompletedTask.Instance;
                };

                await managedClient.SubscribeAsync("topic");

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
                var managedClientOptions = testEnvironment.CreateDefaultManagedMqttClientOptions();
                managedClientOptions.ClientOptions.CleanSession = false;
                await managedClient.StartAsync(managedClientOptions);

========
                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithAutoReconnectDelay(TimeSpan.FromSeconds(0.5)).Build());
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
                await LongTestDelay();

                Assert.AreEqual(1, receivedManagedMessages.Count);

                await managedClient.StopAsync();
                await LongTestDelay();

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
                await managedClient.StartAsync(managedClientOptions);

========
                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithAutoReconnectDelay(TimeSpan.FromSeconds(0.5)).Build());
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
                await LongTestDelay();

                // After reconnect and then some delay, the retained message must not be received,
                // showing that the subscriptions were cleared
                Assert.AreEqual(1, receivedManagedMessages.Count);

                // Make sure that it gets received after subscribing again.
                await managedClient.SubscribeAsync("topic");
                await LongTestDelay();
                
                Assert.AreEqual(2, receivedManagedMessages.Count);
            }
        }

        // This case also serves as a regression test for the previous behavior
        // that subscriptions were only published at the ConnectionCheckInterval
        [TestMethod]
        public async Task Subscriptions_Are_Published_Immediately()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                
                // Use a long connection check interval to verify that the subscriptions
                // do not depend on the connection check interval anymore
                var connectionCheckInterval = TimeSpan.FromSeconds(10);
                var receivingClient = await CreateManagedClientAsync(testEnvironment, null, connectionCheckInterval);
                var sendingClient = await testEnvironment.ConnectClient();

                await sendingClient.PublishAsync(new MqttApplicationMessage { Topic = "topic", Payload = new byte[] { 1 }, Retain = true });

                var subscribeTime = DateTime.UtcNow;

                var messagesTask = SetupReceivingOfMessages(receivingClient, 1);

                await receivingClient.SubscribeAsync("topic");

                var messages = await messagesTask;

                var elapsed = DateTime.UtcNow - subscribeTime;
                Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Subscriptions must be activated immediately, this one took {elapsed}");
                Assert.AreEqual(messages.Single().Topic, "topic");
            }
        }

        // This case also serves as a regression test for the previous behavior which re-published
        // each and every existing subscriptions with every new subscription that was made
        // (causing performance problems and having the visible symptom of retained messages being received again)
        [TestMethod]
        public async Task Subscriptions_Subscribe_Only_New_Subscriptions()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                
                var managedClient = await CreateManagedClientAsync(testEnvironment);

                var sendingClient = await testEnvironment.ConnectClient();

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

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
        async Task<IManagedMqttClient> CreateManagedClientAsync(
========
        async Task<MQTTnet.Extensions.ManagedClient.ManagedMqttClient> CreateManagedClientAsync(
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
            TestEnvironment testEnvironment,
            IMqttClient underlyingClient = null,
            TimeSpan? connectionCheckInterval = null,
            string host = "localhost")
        {
            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer(host, testEnvironment.ServerPort);

            var managedOptions = new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build();

            // Use a short connection check interval so that subscription operations are performed quickly
            // in order to verify against a previous implementation that performed subscriptions only
            // at connection check intervals
            managedOptions.ConnectionCheckInterval = connectionCheckInterval ?? TimeSpan.FromSeconds(0.1);

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
            var managedClient = testEnvironment.CreateManagedMqttClient(underlyingClient ?? testEnvironment.CreateClient());
========
            var managedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(underlyingClient ?? testEnvironment.CreateClient(), new MqttNetEventLogger());
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs

            var connected = GetConnectedTask(managedClient);

            await managedClient.StartAsync(managedOptions);
            
            await connected;

            return managedClient;
        }

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
        static Task GetConnectedTask(IManagedMqttClient managedClient)
========
        /// <summary>
        ///     Returns a task that will finish when the
        ///     <paramref
        ///         name="managedClient" />
        ///     has connected
        /// </summary>
        Task GetConnectedTask(MQTTnet.Extensions.ManagedClient.ManagedMqttClient managedClient)
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
        {
            var promise = new TaskCompletionSource<bool>();

            managedClient.ConnectedAsync += e =>
            {
                promise.TrySetResult(true);
                return CompletedTask.Instance;
            };

            return promise.Task;
        }

<<<<<<<< HEAD:Source/MQTTnet.Tests/ManagedMqttClient/ManagedMqttClient_Connection_Tests.cs
        Task<List<MqttApplicationMessage>> SetupReceivingOfMessages(IManagedMqttClient managedClient, int expectedNumberOfMessages)
========
        /// <summary>
        ///     Returns a task that will return the messages received on
        ///     <paramref
        ///         name="managedClient" />
        ///     when
        ///     <paramref
        ///         name="expectedNumberOfMessages" />
        ///     have been received
        /// </summary>
        Task<List<MqttApplicationMessage>> SetupReceivingOfMessages(MQTTnet.Extensions.ManagedClient.ManagedMqttClient managedClient, int expectedNumberOfMessages)
>>>>>>>> master:Source/MQTTnet.Tests/Clients/ManagedMqttClient/ManagedMqttClient_Tests.cs
        {
            var receivedMessages = new List<MqttApplicationMessage>();
            var result = new AsyncTaskCompletionSource<List<MqttApplicationMessage>>();

            managedClient.ApplicationMessageReceivedAsync += e =>
            {
                receivedMessages.Add(e.ApplicationMessage);

                if (receivedMessages.Count == expectedNumberOfMessages)
                {
                    result.TrySetResult(receivedMessages);
                }

                return CompletedTask.Instance;
            };

            return result.Task;
        }
    }

    public class ManagedMqttClientTestStorage : IManagedMqttClientStorage
    {
        IList<ManagedMqttApplicationMessage> _messages = new List<ManagedMqttApplicationMessage>();

        public int GetMessageCount()
        {
            return _messages.Count;
        }

        public Task<IList<ManagedMqttApplicationMessage>> LoadQueuedMessagesAsync()
        {
            return Task.FromResult(_messages);
        }

        public Task SaveQueuedMessagesAsync(IList<ManagedMqttApplicationMessage> messages)
        {
            _messages = messages;
            return CompletedTask.Instance;
        }
    }
}