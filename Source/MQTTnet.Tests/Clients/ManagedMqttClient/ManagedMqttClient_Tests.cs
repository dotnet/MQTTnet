// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;
using MqttPendingMessagesOverflowStrategy = MQTTnet.Extensions.ManagedClient.MqttPendingMessagesOverflowStrategy;

namespace MQTTnet.Tests.Clients.ManagedMqttClient;

[TestClass]
public sealed class ManagedMqttClient_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Connect_To_Invalid_Server()
    {
        using (var testEnvironment = CreateTestEnvironment())
        {
            testEnvironment.IgnoreClientLogErrors = true;

            var mqttClient = testEnvironment.CreateClient();

            var managedClient = testEnvironment.ClientFactory.CreateManagedMqttClient(mqttClient);

            ConnectingFailedEventArgs connectingFailedEventArgs = null;
            managedClient.ConnectingFailedAsync += args =>
            {
                connectingFailedEventArgs = args;
                return CompletedTask.Instance;
            };

            await managedClient.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = testEnvironment.ClientFactory.CreateClientOptionsBuilder().WithTimeout(TimeSpan.FromSeconds(2)).WithTcpServer("wrong_server", 1234).Build()
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
    public async Task Drop_New_Messages_On_Full_Queue()
    {
        var factory = new MqttClientFactory();
        var managedClient = factory.CreateManagedMqttClient();
        try
        {
            var clientOptions = new ManagedMqttClientOptionsBuilder().WithMaxPendingMessages(5)
                .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage);

            clientOptions.WithClientOptions(o => o.WithTcpServer("localhost"));

            await managedClient.StartAsync(clientOptions.Build());

            await managedClient.EnqueueAsync("1");
            await managedClient.EnqueueAsync("2");
            await managedClient.EnqueueAsync("3");
            await managedClient.EnqueueAsync("4");
            await managedClient.EnqueueAsync("5");
            await managedClient.EnqueueAsync("6");
            await managedClient.EnqueueAsync("7");
            await managedClient.EnqueueAsync("8");

            Assert.AreEqual(5, managedClient.PendingApplicationMessagesCount);
        }
        finally
        {
            await managedClient.StopAsync();
        }
    }

    [TestMethod]
    public async Task Expose_Custom_Connection_Error()
    {
        using (var testEnvironment = CreateTestEnvironment())
        {
            var server = await testEnvironment.StartServer();

            server.ValidatingConnectionAsync += args =>
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return CompletedTask.Instance;
            };

            var managedClient = testEnvironment.ClientFactory.CreateManagedMqttClient();

            MqttClientDisconnectedEventArgs disconnectedArgs = null;
            managedClient.DisconnectedAsync += args =>
            {
                disconnectedArgs = args;
                return CompletedTask.Instance;
            };

            var clientOptions = testEnvironment.ClientFactory.CreateManagedMqttClientOptionsBuilder().WithClientOptions(testEnvironment.CreateDefaultClientOptions()).Build();
            await managedClient.StartAsync(clientOptions);

            await LongTestDelay();

            Assert.IsNotNull(disconnectedArgs);
            Assert.AreEqual(MqttClientConnectResultCode.BadUserNameOrPassword, disconnectedArgs.ConnectResult.ResultCode);
        }
    }

    [TestMethod]
    public async Task ManagedClients_CanInterceptPublishedMessage_AllowingPublish()
    {
        using (var testEnvironment = CreateTestEnvironment())
        {
            await testEnvironment.StartServer();

            var receivedMessagesCount = 0;
            var interceptedMessagesCount = 0;

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build();

            var sendingClient = testEnvironment.CreateClient();
            var sendingManagedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(sendingClient, testEnvironment.ClientLogger);

            await sendingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

            // Wait until the managed client is fully set up and running.
            await Task.Delay(1000);

            var receivingClient = await testEnvironment.ConnectClient();

            receivingClient.ApplicationMessageReceivedAsync += e =>
            {
                Interlocked.Increment(ref receivedMessagesCount);
                return CompletedTask.Instance;
            };
            sendingManagedClient.InterceptPublishMessageAsync += e =>
            {
                Interlocked.Increment(ref interceptedMessagesCount);
                e.AcceptPublish = true;
                return CompletedTask.Instance;
            };

            await receivingClient.SubscribeAsync("My/last/will");

            // Wait for arrival of the will message at the receiver.
            await Task.Delay(5000);

            await sendingManagedClient.EnqueueAsync("My/last/will", "hello world");

            // Wait for arrival of the will message at the receiver.
            await Task.Delay(5000);

            Assert.AreEqual(1, receivedMessagesCount);
            Assert.AreEqual(1, interceptedMessagesCount);
        }
    }


    [TestMethod]
    public async Task ManagedClients_CanInterceptPublishedMessage_PreventingPublish()
    {
        using (var testEnvironment = CreateTestEnvironment())
        {
            await testEnvironment.StartServer();

            var receivedMessagesCount = 0;
            var interceptedMessagesCount = 0;

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build();

            var sendingClient = testEnvironment.CreateClient();
            var sendingManagedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(sendingClient, testEnvironment.ClientLogger);

            await sendingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

            // Wait until the managed client is fully set up and running.
            await Task.Delay(1000);

            var receivingClient = await testEnvironment.ConnectClient();

            receivingClient.ApplicationMessageReceivedAsync += e =>
            {
                Interlocked.Increment(ref receivedMessagesCount);
                return CompletedTask.Instance;
            };
            sendingManagedClient.InterceptPublishMessageAsync += e =>
            {
                Interlocked.Increment(ref interceptedMessagesCount);
                e.AcceptPublish = false;
                return CompletedTask.Instance;
            };

            await receivingClient.SubscribeAsync("My/last/will");

            // Wait for arrival of the will message at the receiver.
            await Task.Delay(5000);

            await sendingManagedClient.EnqueueAsync("My/last/will", "hello world");

            // Wait for arrival of the will message at the receiver.
            await Task.Delay(5000);

            Assert.AreEqual(0, receivedMessagesCount);
            Assert.AreEqual(1, interceptedMessagesCount);
        }
    }

    [TestMethod]
    public async Task ManagedClients_Will_Message_Send()
    {
        using (var testEnvironment = CreateTestEnvironment())
        {
            await testEnvironment.StartServer();

            var receivedMessagesCount = 0;

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                .WithWillTopic("My/last/will")
                .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            var dyingClient = testEnvironment.CreateClient();
            var dyingManagedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(dyingClient, testEnvironment.ClientLogger);

            await dyingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

            // Wait until the managed client is fully set up and running.
            await Task.Delay(1000);

            var receivingClient = await testEnvironment.ConnectClient();

            receivingClient.ApplicationMessageReceivedAsync += e =>
            {
                Interlocked.Increment(ref receivedMessagesCount);
                return CompletedTask.Instance;
            };

            await receivingClient.SubscribeAsync("My/last/will");

            // Disposing the client will not sent a DISCONNECT packet so that connection is terminated
            // which will lead to the will publish.
            dyingManagedClient.Dispose();

            // Wait for arrival of the will message at the receiver.
            await Task.Delay(5000);

            Assert.AreEqual(1, receivedMessagesCount);
        }
    }

    [TestMethod]
    public async Task Publish_Does_Not_Hang_On_Server_Error()
    {
        var timeout = TimeSpan.FromSeconds(2);
        var testTimeout = TimeSpan.FromSeconds(timeout.TotalSeconds * 2);

        const string topic = "test_topic_42";

        using (var testEnvironment = CreateTestEnvironment())
        using (var managedClient = await CreateManagedClientAsync(testEnvironment, timeout: timeout))
        {
            testEnvironment.IgnoreClientLogErrors = true;
            var reject = true;
            var receivedOnServer = new TaskCompletionSource<object>();
            managedClient.ApplicationMessageProcessedAsync += e => Task.FromResult(reject &= e.Exception is null);
            testEnvironment.Server.InterceptingInboundPacketAsync += e =>
            {
                if (e.Packet is MqttPublishPacket)
                {
                    if (reject)
                    {
                        e.ProcessPacket = false;
                    }
                    else
                    {
                        receivedOnServer.TrySetResult(null);
                    }
                }

                return CompletedTask.Instance;
            };

            await managedClient.EnqueueAsync(
                new MqttApplicationMessage
                    { Topic = topic, PayloadSequence = new ReadOnlySequence<byte>(new byte[] { 1 }), Retain = true, QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

            var timeoutTask = Task.Delay(testTimeout);

            var firstDone = await Task.WhenAny(receivedOnServer.Task, timeoutTask);
            Assert.AreEqual(receivedOnServer.Task, firstDone, "Client is hung on publish!");
        }
    }

    [TestMethod]
    public async Task Receive_While_Not_Cleanly_Disconnected()
    {
        using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
        {
            await testEnvironment.StartServer(o => o.WithPersistentSessions());

            var senderClient = await testEnvironment.ConnectClient();

            // Prepare managed client.
            var managedClient = testEnvironment.ClientFactory.CreateManagedMqttClient();
            await managedClient.SubscribeAsync("#");
            var receivedMessages = testEnvironment.CreateApplicationMessageHandler(managedClient);

            var managedClientOptions = new ManagedMqttClientOptions
            {
                ClientOptions = testEnvironment.ClientFactory.CreateClientOptionsBuilder()
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
    public async Task Start_Stop()
    {
        using (var testEnvironment = CreateTestEnvironment())
        {
            var server = await testEnvironment.StartServer();

            var managedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort);

            var connected = GetConnectedTask(managedClient);

            await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

            await connected;

            await managedClient.StopAsync();

            await Task.Delay(500);

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

            var managedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
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
    public async Task Subscribe_Does_Not_Hang_On_Server_Stop()
    {
        var timeout = TimeSpan.FromSeconds(2);
        var testTimeout = TimeSpan.FromSeconds(timeout.TotalSeconds * 2);
        const string topic = "test_topic_2";
        using (var testEnvironment = CreateTestEnvironment())
        using (var managedClient = await CreateManagedClientAsync(testEnvironment, timeout: timeout))
        {
            testEnvironment.IgnoreClientLogErrors = true;
            var reject = true;
            var receivedOnServer = new SemaphoreSlim(0, 1);
            var failedOnClient = new SemaphoreSlim(0, 1);
            testEnvironment.Server.InterceptingInboundPacketAsync += e =>
            {
                if (e.Packet is MqttSubscribePacket)
                {
                    if (reject)
                    {
                        e.ProcessPacket = false;
                    }

                    receivedOnServer.Release();
                }

                return CompletedTask.Instance;
            };
            managedClient.SynchronizingSubscriptionsFailedAsync += e =>
            {
                failedOnClient.Release();
                return CompletedTask.Instance;
            };

            await managedClient.SubscribeAsync(topic);
            Assert.IsTrue(await receivedOnServer.WaitAsync(testTimeout));
            Assert.IsTrue(await failedOnClient.WaitAsync(testTimeout));

            reject = false;
            await managedClient.SubscribeAsync(topic);
            Assert.IsTrue(await receivedOnServer.WaitAsync(testTimeout));
        }
    }

    [TestMethod]
    public async Task Subscriptions_And_Unsubscriptions_Are_Made_And_Reestablished_At_Reconnect()
    {
        using (var testEnvironment = CreateTestEnvironment())
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
            await Task.Delay(500);

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
            await testEnvironment.StartServer().ConfigureAwait(false);

            var sendingClient = await testEnvironment.ConnectClient().ConfigureAwait(false);
            await sendingClient.PublishStringAsync("topic", "A", retain: true);

            // Wait a bit for the retained message to be available
            await LongTestDelay();

            await sendingClient.DisconnectAsync();

            // Now use the managed client and check if subscriptions get cleared properly.

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort);

            var receivedManagedMessages = new List<MqttApplicationMessage>();

            var managedClient = testEnvironment.ClientFactory.CreateManagedMqttClient(testEnvironment.CreateClient());
            managedClient.ApplicationMessageReceivedAsync += e =>
            {
                receivedManagedMessages.Add(e.ApplicationMessage);
                return CompletedTask.Instance;
            };

            await managedClient.SubscribeAsync("topic");

            await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithAutoReconnectDelay(TimeSpan.FromSeconds(0.5)).Build());
            await LongTestDelay();

            Assert.AreEqual(1, receivedManagedMessages.Count);

            await managedClient.StopAsync();
            await LongTestDelay();

            await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithAutoReconnectDelay(TimeSpan.FromSeconds(0.5)).Build());
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
            // Use a long connection check interval to verify that the subscriptions
            // do not depend on the connection check interval anymore
            var connectionCheckInterval = TimeSpan.FromSeconds(10);
            var receivingClient = await CreateManagedClientAsync(testEnvironment, null, connectionCheckInterval);
            var sendingClient = await testEnvironment.ConnectClient();

            await sendingClient.PublishAsync(new MqttApplicationMessage { Topic = "topic", PayloadSequence = new ReadOnlySequence<byte>(new byte[] { 1 }), Retain = true });

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
            var managedClient = await CreateManagedClientAsync(testEnvironment);

            var sendingClient = await testEnvironment.ConnectClient();

            await managedClient.SubscribeAsync("topic");

            //wait a bit for the subscription to become established
            await Task.Delay(500);

            await sendingClient.PublishAsync(new MqttApplicationMessage { Topic = "topic", PayloadSequence = new ReadOnlySequence<byte>(new byte[] { 1 }), Retain = true });

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

    [TestMethod]
    public async Task Unsubscribe_Does_Not_Hang_On_Server_Stop()
    {
        var timeout = TimeSpan.FromSeconds(2);
        var testTimeout = TimeSpan.FromSeconds(timeout.TotalSeconds * 2);
        const string topic = "test_topic_2";
        using (var testEnvironment = CreateTestEnvironment())
        using (var managedClient = await CreateManagedClientAsync(testEnvironment, timeout: timeout))
        {
            testEnvironment.IgnoreClientLogErrors = true;
            var reject = true;
            var receivedOnServer = new SemaphoreSlim(0, 1);
            var failedOnClient = new SemaphoreSlim(0, 1);
            testEnvironment.Server.InterceptingInboundPacketAsync += e =>
            {
                if (e.Packet is MqttUnsubscribePacket)
                {
                    if (reject)
                    {
                        e.ProcessPacket = false;
                    }

                    receivedOnServer.Release();
                }
                else if (e.Packet is MqttSubscribePacket)
                {
                    receivedOnServer.Release();
                }

                return CompletedTask.Instance;
            };
            managedClient.SynchronizingSubscriptionsFailedAsync += e =>
            {
                failedOnClient.Release();
                return CompletedTask.Instance;
            };

            await managedClient.SubscribeAsync(topic);
            Assert.IsTrue(await receivedOnServer.WaitAsync(testTimeout));

            await managedClient.UnsubscribeAsync(topic);
            Assert.IsTrue(await receivedOnServer.WaitAsync(testTimeout));
            Assert.IsTrue(await failedOnClient.WaitAsync(testTimeout));

            reject = false;
            await managedClient.UnsubscribeAsync(topic);
            Assert.IsTrue(await receivedOnServer.WaitAsync(testTimeout));
        }
    }

    async Task<MQTTnet.Extensions.ManagedClient.ManagedMqttClient> CreateManagedClientAsync(
        TestEnvironment testEnvironment,
        IMqttClient underlyingClient = null,
        TimeSpan? connectionCheckInterval = null,
        string host = "localhost",
        TimeSpan? timeout = null)
    {
        await testEnvironment.StartServer();

        var clientOptions = new MqttClientOptionsBuilder().WithTcpServer(host, testEnvironment.ServerPort);

        if (timeout != null)
        {
            clientOptions.WithTimeout(timeout.Value);
        }

        var managedOptions = new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build();

        // Use a short connection check interval so that subscription operations are performed quickly
        // in order to verify against a previous implementation that performed subscriptions only
        // at connection check intervals
        managedOptions.ConnectionCheckInterval = connectionCheckInterval ?? TimeSpan.FromSeconds(0.1);

        var managedClient = new MQTTnet.Extensions.ManagedClient.ManagedMqttClient(underlyingClient ?? testEnvironment.CreateClient(), new MqttNetEventLogger());

        var connected = GetConnectedTask(managedClient);

        await managedClient.StartAsync(managedOptions);

        await connected;

        return managedClient;
    }

    /// <summary>
    ///     Returns a task that will finish when the
    ///     <paramref
    ///         name="managedClient" />
    ///     has connected
    /// </summary>
    Task GetConnectedTask(MQTTnet.Extensions.ManagedClient.ManagedMqttClient managedClient)
    {
        var connected = new TaskCompletionSource<bool>();

        managedClient.ConnectedAsync += e =>
        {
            connected.TrySetResult(true);
            return CompletedTask.Instance;
        };

        return connected.Task;
    }

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
    IList<ManagedMqttApplicationMessage> _messages;

    public int GetMessageCount()
    {
        return _messages.Count;
    }

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
        return CompletedTask.Instance;
    }
}