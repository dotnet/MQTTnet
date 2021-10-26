using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Subscribe_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Intercept_Subscription()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithSubscriptionInterceptor(
                    c =>
                    {
                        // Set the topic to "a" regards what the client wants to subscribe.
                        c.TopicFilter.Topic = "a";
                    }));

                var topicAReceived = false;
                var topicBReceived = false;

                var client = await testEnvironment.ConnectClient();
                client.UseApplicationMessageReceivedHandler(c =>
                {
                    if (c.ApplicationMessage.Topic == "a")
                    {
                        topicAReceived = true;
                    }
                    else if (c.ApplicationMessage.Topic == "b")
                    {
                        topicBReceived = true;
                    }
                });

                await client.SubscribeAsync("b");

                await client.PublishAsync("a");

                await Task.Delay(500);

                Assert.IsTrue(topicAReceived);
                Assert.IsFalse(topicBReceived);
            }
        }

        [TestMethod]
        public async Task Subscribe_Unsubscribe()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                var server = await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("c1"));
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                var c2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("c2"));

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();
                await c2.PublishAsync(message);

                await Task.Delay(500);
                Assert.AreEqual(0, receivedMessagesCount);

                var subscribeEventCalled = false;
                server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedTopicHandlerDelegate(e =>
                {
                    subscribeEventCalled = e.TopicFilter.Topic == "a" && e.ClientId == c1.Options.ClientId;
                });

                await c1.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
                await Task.Delay(250);
                Assert.IsTrue(subscribeEventCalled, "Subscribe event not called.");

                await c2.PublishAsync(message);
                await Task.Delay(250);
                Assert.AreEqual(1, receivedMessagesCount);

                var unsubscribeEventCalled = false;
                server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(e =>
                {
                    unsubscribeEventCalled = e.TopicFilter == "a" && e.ClientId == c1.Options.ClientId;
                });

                await c1.UnsubscribeAsync("a");
                await Task.Delay(250);
                Assert.IsTrue(unsubscribeEventCalled, "Unsubscribe event not called.");

                await c2.PublishAsync(message);
                await Task.Delay(500);
                Assert.AreEqual(1, receivedMessagesCount);

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Subscribe_Multiple_In_Single_Request()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("a")
                    .WithTopicFilter("b")
                    .WithTopicFilter("c")
                    .Build());

                var c2 = await testEnvironment.ConnectClient();

                await c2.PublishAsync("a");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 1);

                await c2.PublishAsync("b");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 2);

                await c2.PublishAsync("c");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 3);
            }
        }

        [TestMethod]
        public async Task Subscribe_Lots_In_Single_Request()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                var optionsBuilder = new MqttClientSubscribeOptionsBuilder();
                for (var i = 0; i < 500; i++)
                {
                    optionsBuilder.WithTopicFilter(i.ToString());
                }

                await c1.SubscribeAsync(optionsBuilder.Build()).ConfigureAwait(false);

                var c2 = await testEnvironment.ConnectClient();

                var messageBuilder = new MqttApplicationMessageBuilder();
                for (var i = 0; i < 500; i++)
                {
                    messageBuilder.WithTopic(i.ToString());

                    await c2.PublishAsync(messageBuilder.Build()).ConfigureAwait(false);
                }

                SpinWait.SpinUntil(() => receivedMessagesCount == 500, TimeSpan.FromSeconds(20));

                Assert.AreEqual(500, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Subscribe_Lots_In_Multiple_Requests()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                for (var i = 0; i < 500; i++)
                {
                    var so = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(i.ToString()).Build();

                    await c1.SubscribeAsync(so).ConfigureAwait(false);

                    await Task.Delay(10);
                }

                var c2 = await testEnvironment.ConnectClient();

                var messageBuilder = new MqttApplicationMessageBuilder();
                for (var i = 0; i < 500; i++)
                {
                    messageBuilder.WithTopic(i.ToString());

                    await c2.PublishAsync(messageBuilder.Build()).ConfigureAwait(false);

                    await Task.Delay(10);
                }

                SpinWait.SpinUntil(() => receivedMessagesCount == 500, 5000);

                Assert.AreEqual(500, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Subscribe_Multiple_In_Multiple_Request()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("a")
                    .Build());

                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("b")
                    .Build());

                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("c")
                    .Build());

                var c2 = await testEnvironment.ConnectClient();

                await c2.PublishAsync("a");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 1);

                await c2.PublishAsync("b");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 2);

                await c2.PublishAsync("c");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 3);
            }
        }
        
        [TestMethod]
        public async Task Deny_Invalid_Topic()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithSubscriptionInterceptor(
                    c =>
                    {
                        if (c.TopicFilter.Topic == "not_allowed_topic")
                        {
                            c.Response.ReasonCode = MqttSubscribeReasonCode.TopicFilterInvalid;
                        }
                    }));
                
                var client = await testEnvironment.ConnectClient();
                
                var subscribeResult =await client.SubscribeAsync("allowed_topic");
                Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS0, subscribeResult.Items[0].ResultCode);

                subscribeResult =await client.SubscribeAsync("not_allowed_topic");
                Assert.AreEqual(MqttClientSubscribeResultCode.TopicFilterInvalid, subscribeResult.Items[0].ResultCode);
            }
        }
    }
}