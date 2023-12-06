// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Publishing_Tests : BaseTestClass
    {
        [TestMethod]
        [ExpectedException(typeof(MqttClientDisconnectedException))]
        public async Task Disconnect_While_Publishing()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                // The client will be disconnect directly after subscribing!
                server.InterceptingPublishAsync += ev => server.DisconnectClientAsync(ev.ClientId, MqttDisconnectReasonCode.NormalDisconnection);

                var client = await testEnvironment.ConnectClient();
                await client.PublishStringAsync("test", qualityOfServiceLevel: MqttQualityOfServiceLevel.AtLeastOnce);
            }
        }

        [TestMethod]
        public async Task Return_NoMatchingSubscribers_When_Not_Subscribed()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var sender = await testEnvironment.ConnectClient();
                var receiver = await testEnvironment.ConnectClient();

                await receiver.SubscribeAsync("A");

                // AtLeastOnce is required to get an ACK packet from the server.
                var publishResult = await sender.PublishStringAsync("B", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);

                Assert.AreEqual(MqttClientPublishReasonCode.NoMatchingSubscribers, publishResult.ReasonCode);

                Assert.AreEqual(true, publishResult.IsSuccess);
            }
        }

        [TestMethod]
        public async Task Return_Success_When_Subscribed()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var sender = await testEnvironment.ConnectClient();
                var receiver = await testEnvironment.ConnectClient();

                await receiver.SubscribeAsync("A");

                // AtLeastOnce is required to get an ACK packet from the server.
                var publishResult = await sender.PublishStringAsync("A", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);

                Assert.AreEqual(MqttClientPublishReasonCode.Success, publishResult.ReasonCode);

                Assert.AreEqual(true, publishResult.IsSuccess);
            }
        }

        [TestMethod]
        public async Task Intercept_Client_Enqueue()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
                
                var sender = await testEnvironment.ConnectClient();
                
                var receiver = await testEnvironment.ConnectClient();
                await receiver.SubscribeAsync("A");
                var receivedMessages = testEnvironment.CreateApplicationMessageHandler(receiver);
                
                await sender.PublishStringAsync("A", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);

                await LongTestDelay();
                
                receivedMessages.AssertReceivedCountEquals(1);
                
                server.InterceptingClientEnqueueAsync += e =>
                {
                    e.AcceptEnqueue = false;
                    return CompletedTask.Instance;
                };
                
                await sender.PublishStringAsync("A", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);

                await LongTestDelay();
                
                // Do not increase because the internal enqueue to the target client is not accepted!
                receivedMessages.AssertReceivedCountEquals(1);
            }
        }

        [TestMethod]
        public async Task Intercept_Client_Enqueue_Multiple_Clients_Subscribed_Messages_Are_Filtered()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var sender = await testEnvironment.ConnectClient();

                var receiverOne = await testEnvironment.ConnectClient(o => o.WithClientId("One"));
                await receiverOne.SubscribeAsync("A");
                var receivedMessagesOne = testEnvironment.CreateApplicationMessageHandler(receiverOne);

                var receiverTwo = await testEnvironment.ConnectClient(o => o.WithClientId("Two"));
                await receiverTwo.SubscribeAsync("A");
                var receivedMessagesTwo = testEnvironment.CreateApplicationMessageHandler(receiverTwo);

                var receiverThree = await testEnvironment.ConnectClient(o => o.WithClientId("Three"));
                await receiverThree.SubscribeAsync("A");
                var receivedMessagesThree = testEnvironment.CreateApplicationMessageHandler(receiverThree);

                server.InterceptingClientEnqueueAsync += e =>
                {
                    if (e.ReceiverClientId.Contains("Two")) e.AcceptEnqueue = false;
                    return CompletedTask.Instance;
                };

                await sender.PublishStringAsync("A", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);

                await LongTestDelay();

                receivedMessagesOne.AssertReceivedCountEquals(1);
                receivedMessagesTwo.AssertReceivedCountEquals(0);
                receivedMessagesThree.AssertReceivedCountEquals(1);
            }
        }
    }
}