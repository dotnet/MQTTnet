﻿using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Subscription_Identifier_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_Subscription_Identifiers_Supported()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();
                
                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                Assert.IsTrue(connectResult.SubscriptionIdentifiersAvailable);
            }
        }
        
        [TestMethod]
        public async Task Subscribe_With_Subscription_Identifier()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();
                
                var client1 = await testEnvironment.ConnectClient();
                var applicationMessageHandler = testEnvironment.CreateApplicationMessageHandler(client1);
                var topicFilter = testEnvironment.Factory.CreateTopicFilterBuilder().WithTopic("Topic").Build();
                var subscribeOptions = testEnvironment.Factory.CreateSubscribeOptionsBuilder().WithSubscriptionIdentifier(456).WithTopicFilter(topicFilter).Build();
                
                await client1.SubscribeAsync(subscribeOptions);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(0);
                
                // The client will publish a message where it is itself subscribing to.
                await client1.PublishAsync("Topic", "Payload", true);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(1);

                applicationMessageHandler.ReceivedApplicationMessages[0].SubscriptionIdentifiers.Contains(456);
            }
        }
        
        [TestMethod]
        public async Task Subscribe_With_Multiple_Subscription_Identifiers()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();
                
                var client1 = await testEnvironment.ConnectClient();
                var applicationMessageHandler = testEnvironment.CreateApplicationMessageHandler(client1);
                
                var topicFilter = testEnvironment.Factory.CreateTopicFilterBuilder().WithTopic("Topic/A").Build();
                var subscribeOptions = testEnvironment.Factory.CreateSubscribeOptionsBuilder().WithSubscriptionIdentifier(456).WithTopicFilter(topicFilter).Build();
                await client1.SubscribeAsync(subscribeOptions);
                
                await LongTestDelay();
                
                topicFilter = testEnvironment.Factory.CreateTopicFilterBuilder().WithTopic("Topic/+").Build();
                subscribeOptions = testEnvironment.Factory.CreateSubscribeOptionsBuilder().WithSubscriptionIdentifier(789).WithTopicFilter(topicFilter).Build();
                await client1.SubscribeAsync(subscribeOptions);
                
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(0);
                
                // The client will publish a message where it is itself subscribing to.
                await client1.PublishAsync("Topic/A", "Payload", true);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(1);

                applicationMessageHandler.ReceivedApplicationMessages[0].SubscriptionIdentifiers.Contains(456);
                applicationMessageHandler.ReceivedApplicationMessages[0].SubscriptionIdentifiers.Contains(789);
            }
        }
    }
}