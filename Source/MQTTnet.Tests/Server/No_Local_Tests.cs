// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class No_Local_Tests : BaseTestClass
    {
        [TestMethod]
        public Task Subscribe_With_No_Local()
        {
            return ExecuteTest(true, 0);
        }
        
        [TestMethod]
        public Task Subscribe_Without_No_Local()
        {
            return ExecuteTest(false, 1);
        }

        async Task ExecuteTest(
            bool noLocal,
            int expectedCountAfterPublish)
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();
                
                var client1 = await testEnvironment.ConnectClient();
                var applicationMessageHandler = testEnvironment.CreateApplicationMessageHandler(client1);
                var topicFilter = testEnvironment.ClientFactory.CreateTopicFilterBuilder().WithTopic("Topic").WithNoLocal(noLocal).Build();
                await client1.SubscribeAsync(topicFilter);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(0);
                
                // The client will publish a message where it is itself subscribing to.
                await client1.PublishStringAsync("Topic", "Payload", retain: true);
                await LongTestDelay();
                
                applicationMessageHandler.AssertReceivedCountEquals(expectedCountAfterPublish);
            }
        }
    }
}