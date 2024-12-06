// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Retain_Handling_Tests : BaseTestClass
    {
        [TestMethod]
        public Task Send_At_Subscribe()
        {
            return ExecuteTest(MqttRetainHandling.SendAtSubscribe, 1, 2, 3);
        }

        [TestMethod]
        public Task Do_Not_Send_On_Subscribe()
        {
            return ExecuteTest(MqttRetainHandling.DoNotSendOnSubscribe, 0, 1, 1);
        }

        [TestMethod]
        public Task Send_At_Subscribe_If_New_Subscription_Only()
        {
            return ExecuteTest(MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly, 1, 2, 2);
        }

        async Task ExecuteTest(
            MqttRetainHandling retainHandling,
            int expectedCountAfterSubscribe,
            int expectedCountAfterSecondPublish,
            int expectedCountAfterSecondSubscribe)
        {
            using var testEnvironments = CreateMixedTestEnvironment(MqttProtocolVersion.V500);
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var client1 = await testEnvironment.ConnectClient();
                await client1.PublishStringAsync("Topic", "Payload", retain: true);

                await LongTestDelay();

                var client2 = await testEnvironment.ConnectClient();
                var applicationMessageHandler = testEnvironment.CreateApplicationMessageHandler(client2);

                var topicFilter = testEnvironment.ClientFactory.CreateTopicFilterBuilder().WithTopic("Topic").WithRetainHandling(retainHandling).Build();
                await client2.SubscribeAsync(topicFilter);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(expectedCountAfterSubscribe);

                await client1.PublishStringAsync("Topic", "Payload", retain: true);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(expectedCountAfterSecondPublish);

                await client2.SubscribeAsync(topicFilter);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(expectedCountAfterSecondSubscribe);
            }
        }
    }
}