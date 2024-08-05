// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Retain_As_Published_Tests : BaseTestClass
    {
        [TestMethod]
        public Task Subscribe_With_Retain_As_Published()
        {
            return ExecuteTest(true);
        }

        [TestMethod]
        public Task Subscribe_Without_Retain_As_Published()
        {
            return ExecuteTest(false);
        }

        async Task ExecuteTest(bool retainAsPublished)
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var client1 = await testEnvironment.ConnectClient();
                var applicationMessageHandler = testEnvironment.CreateApplicationMessageHandler(client1);
                var topicFilter = testEnvironment.ClientFactory.CreateTopicFilterBuilder().WithTopic("Topic").WithRetainAsPublished(retainAsPublished).Build();
                await client1.SubscribeAsync(topicFilter);
                await LongTestDelay();

                // The client will publish a message where it is itself subscribing to.
                await client1.PublishStringAsync("Topic", "Payload", retain: true);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(1);
                Assert.AreEqual(retainAsPublished, applicationMessageHandler.ReceivedEventArgs[0].ApplicationMessage.Retain);
            }
        }
    }
}