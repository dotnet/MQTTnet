using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.MQTTv5
{
    [TestClass]
    public sealed class Client_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Connect_With_New_Mqtt_Features()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                // This test can be also executed against "broker.hivemq.com" to validate package format.
                var client = await testEnvironment.ConnectClient(
                    new MqttClientOptionsBuilder()
                        //.WithTcpServer("broker.hivemq.com")
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .WithProtocolVersion(MqttProtocolVersion.V500)
                        .WithTopicAliasMaximum(20)
                        .WithReceiveMaximum(20)
                        .WithWillMessage(new MqttApplicationMessageBuilder().WithTopic("abc").Build())
                        .WithWillDelayInterval(20)
                        .Build());

                MqttApplicationMessage receivedMessage = null;

                await client.SubscribeAsync("a");
                client.UseApplicationMessageReceivedHandler(context => { receivedMessage = context.ApplicationMessage; });

                await client.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic("a")
                    .WithPayload("x")
                    .WithUserProperty("a", "1")
                    .WithUserProperty("b", "2")
                    .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
                    .WithAtLeastOnceQoS()
                    .Build());

                await Task.Delay(500);

                Assert.IsNotNull(receivedMessage);

                Assert.AreEqual(2, receivedMessage.UserProperties.Count);
            }
        }
        
        [TestMethod]
        public async Task Connect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500).Build());
            }
        }

        [TestMethod]
        public async Task Connect_And_Disconnect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
                await client.DisconnectAsync();
            }
        }

        [TestMethod]
        public async Task Subscribe()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client =
                    await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));

                var result = await client.SubscribeAsync(new MqttClientSubscribeOptions()
                {
                    SubscriptionIdentifier = 1,
                    TopicFilters = new List<MqttTopicFilter>
                    {
                        new MqttTopicFilter {Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce}
                    }
                });

                await client.DisconnectAsync();

                Assert.AreEqual(1, result.Items.Count);
                Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS1, result.Items[0].ResultCode);
            }
        }

        [TestMethod]
        public async Task Unsubscribe()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
                await client.SubscribeAsync("a");

                var result = await client.UnsubscribeAsync("a");
                await client.DisconnectAsync();

                Assert.AreEqual(1, result.Items.Count);
                Assert.AreEqual(MqttClientUnsubscribeResultCode.Success, result.Items[0].ResultCode);
            }
        }

        [TestMethod]
        public async Task Publish_QoS_0()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
                var result = await client.PublishAsync("a", "b");
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
        }

        [TestMethod]
        public async Task Publish_QoS_1()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
                var result = await client.PublishAsync("a", "b", MqttQualityOfServiceLevel.AtLeastOnce);
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
        }

        [TestMethod]
        public async Task Publish_QoS_2()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
                var result = await client.PublishAsync("a", "b", MqttQualityOfServiceLevel.ExactlyOnce);
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
        }

        [TestMethod]
        public async Task Publish_With_Properties()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("Hello")
                    .WithPayload("World")
                    .WithAtMostOnceQoS()
                    .WithUserProperty("x", "1")
                    .WithUserProperty("y", "2")
                    .WithResponseTopic("response")
                    .WithContentType("text")
                    .WithMessageExpiryInterval(50)
                    .WithCorrelationData(new byte[12])
                    .WithTopicAlias(2)
                    .Build();

                var result = await client.PublishAsync(applicationMessage);
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
        }

        [TestMethod]
        public async Task Subscribe_And_Publish()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

                var client1 = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500).WithClientId("client1"));
                client1.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e);
                    }
                });

                await client1.SubscribeAsync("a");

                var client2 = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500).WithClientId("client2"));
                await client2.PublishAsync("a", "b");

                await Task.Delay(500);

                await client2.DisconnectAsync();
                await client1.DisconnectAsync();

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.AreEqual("Subscribe_And_Publish_client1", receivedMessages[0].ClientId);
                Assert.AreEqual("a", receivedMessages[0].ApplicationMessage.Topic);
                Assert.AreEqual("b", receivedMessages[0].ApplicationMessage.ConvertPayloadToString());
            }
        }

        [TestMethod]
        public async Task Publish_And_Receive_New_Properties()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var receiver = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                await receiver.SubscribeAsync("#");

                MqttApplicationMessage receivedMessage = null;
                receiver.UseApplicationMessageReceivedHandler(c =>
                {
                    receivedMessage = c.ApplicationMessage;
                });

                var sender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("Hello")
                    .WithPayload("World")
                    .WithAtMostOnceQoS()
                    .WithUserProperty("x", "1")
                    .WithUserProperty("y", "2")
                    .WithResponseTopic("response")
                    .WithContentType("text")
                    .WithMessageExpiryInterval(50)
                    .WithCorrelationData(new byte[12])
                    .WithTopicAlias(2)
                    .Build();

                await sender.PublishAsync(applicationMessage);

                await Task.Delay(500);

                Assert.IsNotNull(receivedMessage);
                Assert.AreEqual(applicationMessage.Topic, receivedMessage.Topic);
                Assert.AreEqual(applicationMessage.TopicAlias, receivedMessage.TopicAlias);
                Assert.AreEqual(applicationMessage.ContentType, receivedMessage.ContentType);
                Assert.AreEqual(applicationMessage.ResponseTopic, receivedMessage.ResponseTopic);
                Assert.AreEqual(applicationMessage.MessageExpiryInterval, receivedMessage.MessageExpiryInterval);
                CollectionAssert.AreEqual(applicationMessage.CorrelationData, receivedMessage.CorrelationData);
                CollectionAssert.AreEqual(applicationMessage.Payload, receivedMessage.Payload);
                CollectionAssert.AreEqual(applicationMessage.UserProperties, receivedMessage.UserProperties);
            }
        }
    }
}
