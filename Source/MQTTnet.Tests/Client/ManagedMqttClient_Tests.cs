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
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;
using MqttClient = MQTTnet.Client.MqttClient;

namespace MQTTnet.Tests.Client
{
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

                var managedClient = testEnvironment.Factory.CreateManagedMqttClient(mqttClient);

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
            var factory = new MqttFactory();
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
                var dyingManagedClient = new ManagedMqttClient(dyingClient, testEnvironment.ClientLogger);

                await dyingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build());

                // Wait until the managed client is fully set up and running.
                await Task.Delay(1000);

                var receivingClient = await testEnvironment.ConnectClient();

                receivingClient.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return PlatformAbstractionLayer.CompletedTask;
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
        public async Task Start_Stop()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var managedClient = new ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
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

                var managedClient = new ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
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
        /// <summary>
        /// testing subscribe/unsubscribe at reconnect with subscriptions having NoLocal set.
        /// </summary>
        /// <remarks>
        /// 5/3/2022 jkohler@gpd-global.com
        /// The MQTTnet server doesn't appear to support the NoLocal flag on subscriptions????  
        /// I was unable to get it work as expected.  So this test was developed (naively) to use a mosquitto
        /// broker running on a remote host.
        /// broker:     mosquitto   2.0.12
        /// host:       10.0.2.92:1883  
        ///             Debian 10.11
        /// This test uses a technique seen in other places in this test suite:
        ///   a low level MqttClient is supplied to a ManagedMqttClient.  THe low level
        ///   client can be disconnected to trigger the ManagedClient performing a re-connect
        ///   operation - which issues all current subscriptions.  Which is what we're testing 
        ///   here.
        /// </remarks>
        [TestMethod]
        public async Task Subscriptions_And_Unsubscriptions_Are_Made_And_Reestablished_At_Reconnect_NoLocalPreserved()
        {
            const string TestBrokerAddress = "10.0.2.92";
            const int TestBrokerPort = 1883;
            // 
            // topic which is subscribed _without_ the NoLocal property set.  We _should_ get
            // messages back on this topic.
            const string TopicLocal = "test-topics/nolocal/nolocal-reset";
            //
            // topic which is subscribed _with_ the NoLocal proeprty set.  We should get no
            // messages back on this topic.
            const string TopicNoLocal = "test-topics/nolocal/nolocal-set";

            const int ExpectedReceivedMessageCount = 3;

            var mqttClientFactory = new MqttFactory();

            //
            // create unmanaged client to serve as underlyung client for managed client
            //  an unmanaged client can be connected/disconnected w/o interfering with the 
            //  managed client's 'started' state
            var unmanagedClient = mqttClientFactory.CreateMqttClient();
            var unmanagedClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer( TestBrokerAddress, TestBrokerPort )
                .WithClientId( "NoLocalTest" + Guid.NewGuid().ToString( "N" ) )
                // The NoLocal property on subscriptions is a protocol version 5 thing so
                // this has to be set.
                .WithProtocolVersion( MQTTnet.Formatter.MqttProtocolVersion.V500 )
                .Build();
            //
            // connect the unmanaged client and give a few msecs to stabilize
            var response = await unmanagedClient.ConnectAsync( unmanagedClientOptions, CancellationToken.None );
            await Task.Delay( 500 );
            Assert.IsTrue( unmanagedClient.IsConnected );
            //
            // create a managed client using the unmanaged client 
            // the managed client has to be started
            var managedOptions = new ManagedMqttClientOptionsBuilder().WithClientOptions( unmanagedClientOptions ).Build();
            var managedClient = mqttClientFactory.CreateManagedMqttClient( unmanagedClient ) as ManagedMqttClient;
            Assert.IsNotNull( managedClient );
            //
            // add subscriptions before starting the managedclient - these subscriptions should be sent to the 
            // broker as part of the startup
            await managedClient.SubscribeAsync( TopicLocal, noLocal: false );
            await managedClient.SubscribeAsync( TopicNoLocal, noLocal: true );

            await managedClient.StartAsync( managedOptions );
            await Task.Delay( 500 );
            Assert.IsTrue( managedClient.IsConnected );
            Assert.IsTrue( managedClient.IsStarted );
            //
            // we're connected, started and should have established two subscriptions
            // now we send messages to both subscribed topics - we should see message back
            // on only one topic
            //

            //
            // establish a one-time receiver for expected message count
            Task<List<MqttApplicationMessage>> received = SetupReceivingOfMessages( managedClient, ExpectedReceivedMessageCount );
            //
            // task to publish messages 
            async Task PublishMessages()
            {
                await unmanagedClient.PublishBinaryAsync( TopicLocal, new byte[] { 1 } );       // will receive     +1
                await unmanagedClient.PublishBinaryAsync( TopicNoLocal, new byte[] { 1 } );     // won't receive
                await unmanagedClient.PublishBinaryAsync( TopicNoLocal, new byte[] { 1 } );     // won't receive
                await unmanagedClient.PublishBinaryAsync( TopicLocal, new byte[] { 1 } );       // will receive     +2
                await unmanagedClient.PublishBinaryAsync( TopicNoLocal, new byte[] { 1 } );     // won't receive
                await unmanagedClient.PublishBinaryAsync( TopicNoLocal, new byte[] { 1 } );     // won't receive
                await unmanagedClient.PublishBinaryAsync( TopicLocal, new byte[] { 1 } );       // will receive     +3
            }
            //
            // publish messages
            await PublishMessages();
            //
            // receive 3 messages
            var received_messages = await received;
            //
            // validate count and topics
            Assert.AreEqual( ExpectedReceivedMessageCount, received_messages.Count );
            Assert.IsTrue( received_messages.All( m => m.Topic == TopicLocal ) );
            //
            // now we can disconnect the unmanaged client and the managed client will immediately 
            // begin a reconnect 
            // when disconnected both clients should report ! IsConnected but the managed client will still be 'Started'
            await unmanagedClient.DisconnectAsync();
            Assert.IsFalse( unmanagedClient.IsConnected );
            Assert.IsFalse( managedClient.IsConnected );
            Assert.IsTrue( managedClient.IsStarted );
            //
            // allow some time for the managed client to reconnect and emit the subscriptions
            await Task.Delay( 1000 );
            Assert.IsTrue( unmanagedClient.IsConnected );
            Assert.IsTrue( managedClient.IsConnected );
            Assert.IsTrue( managedClient.IsStarted );
            //
            // setup a message receiver - for two messages
            received = SetupReceivingOfMessages( managedClient, ExpectedReceivedMessageCount );
            // 
            // publish test messages
            await PublishMessages();
            received_messages = await received;
            //
            // validate as before
            Assert.AreEqual( ExpectedReceivedMessageCount, received_messages.Count );
            Assert.IsTrue( received_messages.All( m => m.Topic == TopicLocal ) );
            //
            // graceful shutdown
            await managedClient.StopAsync();
        }

        [TestMethod]
        public async Task Subscriptions_Are_Cleared_At_Logout()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer().ConfigureAwait(false);

                var sendingClient = await testEnvironment.ConnectClient().ConfigureAwait(false);
                await sendingClient.PublishAsync(
                    new MqttApplicationMessage
                    {
                        Topic = "topic",
                        Payload = new byte[] { 1 },
                        Retain = true
                    });

                // Wait a bit for the retained message to be available
                await Task.Delay(1000);

                await sendingClient.DisconnectAsync();

                // Now use the managed client and check if subscriptions get cleared properly.

                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort);

                var receivedManagedMessages = new List<MqttApplicationMessage>();
                var managedClient = new ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetEventLogger());
                managedClient.ApplicationMessageReceivedAsync += e =>
                {
                    receivedManagedMessages.Add(e.ApplicationMessage);
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await managedClient.SubscribeAsync("topic");

                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithAutoReconnectDelay(TimeSpan.FromSeconds(1)).Build());

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedManagedMessages.Count);

                await managedClient.StopAsync();

                await Task.Delay(500);

                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).WithAutoReconnectDelay(TimeSpan.FromSeconds(1)).Build());

                await Task.Delay(1000);

                // After reconnect and then some delay, the retained message must not be received,
                // showing that the subscriptions were cleared
                Assert.AreEqual(1, receivedManagedMessages.Count);

                // Make sure that it gets received after subscribing again.
                await managedClient.SubscribeAsync("topic");
                await Task.Delay(500);
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

        async Task<ManagedMqttClient> CreateManagedClientAsync(
            TestEnvironment testEnvironment,
            IMqttClient underlyingClient = null,
            TimeSpan? connectionCheckInterval = null,
            string host = "localhost")
        {
            await testEnvironment.StartServer();

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer(host, testEnvironment.ServerPort);

            var managedOptions = new ManagedMqttClientOptionsBuilder().WithClientOptions(clientOptions).Build();

            // Use a short connection check interval so that subscription operations are performed quickly
            // in order to verify against a previous implementation that performed subscriptions only
            // at connection check intervals
            managedOptions.ConnectionCheckInterval = connectionCheckInterval ?? TimeSpan.FromSeconds(0.1);

            var managedClient = new ManagedMqttClient(underlyingClient ?? testEnvironment.CreateClient(), new MqttNetEventLogger());

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
        Task GetConnectedTask( ManagedMqttClient managedClient )
        {
            var connected = new TaskCompletionSource<bool>();

            managedClient.ConnectedAsync += e => {
                connected.TrySetResult( true );
                return PlatformAbstractionLayer.CompletedTask;
            };

            return connected.Task;
        }
        /// <summary>
        ///     Returns a task that will finish when the
        ///     <paramref
        ///         name="managedClient" />
        ///     has disconnected
        /// </summary>
        Task GetDisConnectedTask( ManagedMqttClient managedClient )
        {
            var connected = new TaskCompletionSource<bool>();

            managedClient.DisconnectedAsync += e => {
                connected.TrySetResult( false );
                return PlatformAbstractionLayer.CompletedTask;
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
        Task<List<MqttApplicationMessage>> SetupReceivingOfMessages(ManagedMqttClient managedClient, int expectedNumberOfMessages)
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

                return PlatformAbstractionLayer.CompletedTask;
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
            return Task.FromResult(0);
        }
    }
}